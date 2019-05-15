using System;
using System.Collections.Generic;
using System.Data;
using CastIron.Sql.Execution;
using CastIron.Sql.Utility;

namespace CastIron.Sql.Mapping
{
    public abstract class DataReaderResultsBase : IDataResultsBase
    {       
        protected const string StateRawReaderConsumed = "RawReaderConsumed";
        protected const string StateReaderConsuming = "ReaderConsuming";
        protected const string StateOutputParams = "OutputParams";

        protected StringKeyedStateMachine StateMachine { get; }
        protected IDbCommand Command { get; }
        protected IExecutionContext Context { get; }
        protected IDataReader Reader { get;  }
        private readonly int? _rowsAffected;

        private ParameterCache _parameterCache;

        protected DataReaderResultsBase(IDbCommand command, IExecutionContext context, IDataReader reader, int? rowsAffected)
        {
            Command = command;
            Context = context;
            Reader = reader;
            CurrentSet = 0;
            _rowsAffected = rowsAffected;

            StateMachine = new StringKeyedStateMachine();
            StateMachine.AddState(StateRawReaderConsumed)
                .TransitionOnEvent(StateRawReaderConsumed, null, () => throw DataReaderException.ThrowRawReaderConsumedException())
                .TransitionOnEvent(StateReaderConsuming, null, () => throw DataReaderException.ThrowRawReaderConsumedException())
                .TransitionOnEvent(StateOutputParams, null, () => throw DataReaderException.ThrowRawReaderConsumedException());
            StateMachine.AddState(StateReaderConsuming)
                .TransitionOnEvent(StateRawReaderConsumed, null, () => throw DataReaderException.ThrowConsumeAfterStreamingStartedException())
                .TransitionOnEvent(StateReaderConsuming, StateReaderConsuming)
                .TransitionOnEvent(StateOutputParams, StateOutputParams, EnableOutputParameters);
            StateMachine.AddState(StateOutputParams)
                .TransitionOnEvent(StateRawReaderConsumed, null, () => throw DataReaderException.ThrowReaderClosedException())
                .TransitionOnEvent(StateReaderConsuming, null, () => throw DataReaderException.ThrowReaderClosedException())
                .TransitionOnEvent(StateOutputParams, StateOutputParams, EnableOutputParameters);
        }

        public int CurrentSet { get; private set; }

        public int RowsAffected => _rowsAffected ?? Reader?.RecordsAffected ?? 0;

        public IDataReader AsRawReader()
        {
            StateMachine.ReceiveEvent(StateRawReaderConsumed);
            return Reader;
        }

        private void EnableOutputParameters()
        {
            if (Reader == null)
                return;
            if (!Reader.IsClosed)
                Reader.Close();
        }

        public ParameterCache GetParameters()
        {
            StateMachine.ReceiveEvent(StateOutputParams);
            if (_parameterCache == null)
                _parameterCache = new ParameterCache(Command);
            return _parameterCache;
        }

        public IEnumerable<T> AsEnumerable<T>(Action<IMapCompilerBuilder<T>> setup = null)
        {
            StateMachine.ReceiveEvent(StateReaderConsuming);
            if (CurrentSet == 0)
                CurrentSet = 1;
            var context = new MapCompilerBuilder<T>();
            setup?.Invoke(context);
            var map = context.Compile(Reader);
            return new DataRecordMappingEnumerable<T>(Reader, Context, map);
        }

        public IDataResultsBase AdvanceToResultSet(int num)
        {
            StateMachine.ReceiveEvent(StateReaderConsuming);

            // TODO: Review all this logic to make sure it is sane and necessary
            if (CurrentSet > num)
                throw new Exception($"Cannot read result sets out of order. At Set={CurrentSet} but requested Set={num}.");
            if (CurrentSet == 0)
            {
                CurrentSet = 1;
                if (num == 1)
                    return this;
            }

            while (CurrentSet < num)
            {
                if (!Reader.NextResult())
                    throw new Exception($"Could not read requested result set {num}. This stream only contains {CurrentSet} result sets.");
                CurrentSet++;
            }

            if (CurrentSet != num)
                throw new Exception($"Could not find result Set={num}. This should not happen.");

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
                if (!Reader.NextResult())
                    return false;
                CurrentSet++;
            }

            if (CurrentSet != num)
                return false;

            return true;
        }
    }

    /// <summary>
    /// Encapsulates both the IDataReader and the IDbCommand to give unified access to all result
    /// sets and output parameters from the command
    /// </summary>
    public class DataReaderResults : DataReaderResultsBase, IDataResults
    {
        public DataReaderResults(IDbCommand command, IExecutionContext context, IDataReader reader, int? rowsAffected = null)
            : base(command, context, reader, rowsAffected)
        {
        }
    }

    public class DataReaderResultsStream : DataReaderResultsBase, IDataResultsStream
    {
        protected const string StateDisposed = "Disposed";

        public DataReaderResultsStream(IDbCommand command, IExecutionContext context, IDataReader reader)
            : base(command, context, reader, null)
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