﻿using System;
using System.Collections.Generic;
using System.Data;
using CastIron.Sql.Execution;
using CastIron.Sql.Mapping.Enumerables;
using CastIron.Sql.Utility;

namespace CastIron.Sql.Mapping
{
    public abstract class DataReaderResultsBase : IDataResultsBase
    {
        protected const string StateRawReaderConsumed = "RawReaderConsumed";
        protected const string StateReaderConsuming = "ReaderConsuming";
        protected const string StateOutputParams = "OutputParams";

        private readonly int? _rowsAffected;
        private readonly object _queryObject;

        private ParameterCache _parameterCache;
        private object _cacheKey;

        protected DataReaderResultsBase(IDbCommandAsync command, IExecutionContext context, IDataReaderAsync reader, int? rowsAffected, object queryObject)
        {
            Command = command;
            Context = context;
            Reader = reader;
            CurrentSet = 0;
            Provider = context.Provider;
            _rowsAffected = rowsAffected;
            _queryObject = queryObject;
            _cacheKey = null;

            StateMachine = new StringKeyedStateMachine();
            StateMachine.AddState(StateRawReaderConsumed)
                .TransitionOnEvent(StateRawReaderConsumed, null, ThrowRawReaderConsumedException)
                .TransitionOnEvent(StateReaderConsuming, null, ThrowRawReaderConsumedException)
                .TransitionOnEvent(StateOutputParams, null, ThrowRawReaderConsumedException);
            StateMachine.AddState(StateReaderConsuming)
                .TransitionOnEvent(StateRawReaderConsumed, null, ThrowConsumeAfterStreamingStartedException)
                .TransitionOnEvent(StateReaderConsuming, StateReaderConsuming)
                .TransitionOnEvent(StateOutputParams, StateOutputParams, EnableOutputParameters);
            StateMachine.AddState(StateOutputParams)
                .TransitionOnEvent(StateRawReaderConsumed, null, ThrowReaderClosedException)
                .TransitionOnEvent(StateReaderConsuming, null, ThrowReaderClosedException)
                .TransitionOnEvent(StateOutputParams, StateOutputParams, EnableOutputParameters);
        }

        protected StringKeyedStateMachine StateMachine { get; }
        protected IDbCommandAsync Command { get; }
        protected IExecutionContext Context { get; }
        protected IDataReaderAsync Reader { get; }
        protected IProviderConfiguration Provider { get; }

        public int CurrentSet { get; private set; }

        public int RowsAffected => _rowsAffected ?? Reader?.Reader?.RecordsAffected ?? 0;

        public void CacheMappings(bool useCache, object key = null)
        {
            _cacheKey = useCache ? (key ?? _queryObject) : null;
        }

        public IDataReader AsRawReader()
        {
            StateMachine.ReceiveEvent(StateRawReaderConsumed);
            return Reader?.Reader;
        }

        private void EnableOutputParameters()
        {
            if (Reader?.Reader == null)
                return;
            if (!Reader.Reader.IsClosed)
                Reader.Reader.Close();
        }

        public ParameterCache GetParameters()
        {
            StateMachine.ReceiveEvent(StateOutputParams);
            if (_parameterCache == null)
                _parameterCache = new ParameterCache(Command.Command);
            return _parameterCache;
        }

        public IEnumerable<T> AsEnumerable<T>(Action<IMapCompilerSettings> setup = null)
        {
            PrepareToEnumerate();
            var map = CreateMap<T>(setup);
            return new DataRecordMappingEnumerable<T>(this, Reader, Context, map);
        }

        protected Func<IDataRecord, T> CreateMap<T>(Action<IMapCompilerSettings> setup)
        {
            var cached = Context.MapCache.Get(_cacheKey, CurrentSet) as Func<IDataRecord, T>;
            if (cached != null)
                return cached;
            var map = CreateMapInternal<T>(setup);
            Context.MapCache.Cache(_cacheKey, CurrentSet, map);
            return map;
        }

        private Func<IDataRecord, T> CreateMapInternal<T>(Action<IMapCompilerSettings> setup)
        {
            var settings = new CompilationSettings();
            var compilerBuilder = new MapCompilerSettings(settings);
            setup?.Invoke(compilerBuilder);
            var operation = new MapContext(Provider, typeof(T), settings);
            var map = Context.MapCompiler.Compile<T>(operation, Reader.Reader);
            return map;
        }

        protected void PrepareToEnumerate()
        {
            StateMachine.ReceiveEvent(StateReaderConsuming);
            if (CurrentSet == 0)
                CurrentSet = 1;
        }

        public IDataResultsBase AdvanceToResultSet(int num)
        {
            StateMachine.ReceiveEvent(StateReaderConsuming);

            if (CurrentSet > num)
                throw DataReaderException.ResultSetsAccessedOutOfOrder(CurrentSet, num);
            if (CurrentSet == 0)
            {
                CurrentSet = 1;
                if (num == 1)
                    return this;
            }

            while (CurrentSet < num)
            {
                if (!Reader.Reader.NextResult())
                    throw DataReaderException.ResultSetIndexOutOfBounds(num, CurrentSet);
                CurrentSet++;
            }

            if (CurrentSet != num)
                throw new DataReaderException($"Could not find result Set={num}. This should not happen.");

            return this;
        }

        public bool TryAdvanceToResultSet(int num)
        {
            StateMachine.ReceiveEvent(StateReaderConsuming);

            if (CurrentSet > num)
                return false;

            if (CurrentSet == 0)
            {
                CurrentSet = 1;
                if (num == 1)
                    return true;
            }

            while (CurrentSet < num)
            {
                if (!Reader.Reader.NextResult())
                    return false;
                CurrentSet++;
            }

            if (CurrentSet != num)
                return false;

            return true;
        }

        private void ThrowRawReaderConsumedException() => throw DataReaderException.RawReaderConsumed();
        private void ThrowConsumeAfterStreamingStartedException() => throw DataReaderException.ConsumeAfterStreamingStarted();
        private void ThrowReaderClosedException() => throw DataReaderException.ReaderClosed();
    }

    /// <summary>
    /// Encapsulates both the IDataReader and the IDbCommand to give unified access to all result
    /// sets and output parameters from the command
    /// </summary>
    public class DataReaderResults : DataReaderResultsBase, IDataResults
    {
        public DataReaderResults(IDbCommandAsync command, IExecutionContext context, IDataReaderAsync reader, int? rowsAffected, object queryObject)
            : base(command, context, reader, rowsAffected, queryObject)
        {
        }
    }

    public sealed class DataReaderResultsStream : DataReaderResultsBase, IDataResultsStream
    {
        private const string StateDisposed = "Disposed";

        public DataReaderResultsStream(IDbCommandAsync command, IExecutionContext context, IDataReaderAsync reader, object queryObject)
            : base(command, context, reader, null, queryObject)
        {
            StateMachine.AddState(StateDisposed)
                .TransitionOnEvent(StateRawReaderConsumed, null, ThrowDisposedException)
                .TransitionOnEvent(StateReaderConsuming, null, ThrowDisposedException)
                .TransitionOnEvent(StateOutputParams, null, ThrowDisposedException)
                .TransitionOnEvent(StateDisposed, StateDisposed);
            StateMachine.UpdateState(StateRawReaderConsumed)
                .TransitionOnEvent(StateDisposed, StateDisposed, () => DisposeReferences(false));
            StateMachine.UpdateState(StateReaderConsuming)
                .TransitionOnEvent(StateDisposed, StateDisposed, () => DisposeReferences(true));
            StateMachine.UpdateState(StateOutputParams)
                .TransitionOnEvent(StateDisposed, StateDisposed, () => DisposeReferences(true));
        }

#if NETSTANDARD2_1

        public IAsyncEnumerable<T> AsEnumerableAsync<T>(Action<IMapCompilerSettings> setup = null)
        {
            PrepareToEnumerate();
            var map = CreateMap<T>(setup);
            return new AsyncDataRecordMappingEnumerable<T>(this, Reader, Context, map);
        }

#endif

        public void Dispose()
        {
            StateMachine.ReceiveEvent(StateDisposed);
        }

        private static void ThrowDisposedException()
        {
            throw new ObjectDisposedException(
                $"The {nameof(DataReaderResults)} and underlying {nameof(IDataReader)} objects " +
                "have been disposed and cannot be used for any other purposes.");
        }

        private void DisposeReferences(bool disposeReader)
        {
            Context?.MarkComplete();
            if (disposeReader)
                Reader.Dispose();
            Command?.Dispose();
            Context?.Dispose();
        }
    }
}