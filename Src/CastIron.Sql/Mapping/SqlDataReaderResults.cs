using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using CastIron.Sql.Debugging;
using CastIron.Sql.Execution;

namespace CastIron.Sql.Mapping
{
    /// <summary>
    /// Encapsulates both the IDataReader and the IDbCommand to give unified access to all result
    /// sets and output parameters from the command
    /// </summary>
    public class SqlDataReaderResults : IDataResults
    {
        private const string StateInitial = "";
        private const string StateRawReaderConsumed = "RawReaderConsumed";
        private const string StateReaderConsuming = "ReaderConsuming";
        private const string StateOutputParams = "OutputParams";
        private const string StateDisposed = "Disposed";

        private static readonly Dictionary<string, Func<IResultState>> _getState = new Dictionary<string, Func<IResultState>>
        {
            { StateRawReaderConsumed, () => new RawReaderConsumedState() },
            { StateReaderConsuming, () => new ReaderConsumingState() },
            { StateOutputParams, () => new OutputParametersState() },
            { StateDisposed, () => new DisposedState() }
        };
        private readonly IDbCommand _command;
        private readonly IExecutionContext _context;
        private readonly IDataReader _reader;

        private IResultState _currentState;

        public SqlDataReaderResults(IDbCommand command, IExecutionContext context, IDataReader reader, int? rowsAffected = null)
        {
            _command = command;
            _context = context;
            _reader = reader;
            _currentState = new InitialState();
            CurrentSet = 0;
            RowsAffected = rowsAffected ?? reader?.RecordsAffected ?? 0;
        }

        public int CurrentSet { get; private set; }
        public int RowsAffected { get; }

        public IDataReader AsRawReader()
        {
            TryTransitionToState(StateRawReaderConsumed);
            return _reader;
        }

        public IDataReader AsRawReaderWithBetterErrorMessages()
        {
            TryTransitionToState(StateRawReaderConsumed);
            return new DataReaderWithBetterErrorMessages(_reader);
        }

        public IEnumerable<T> AsEnumerable<T>(Action<IMapCompilerBuilder<T>> setup = null)
        {
            TryTransitionToState(StateReaderConsuming);
            if (CurrentSet == 0)
                CurrentSet = 1;
            var context = new MapCompilerBuilder<T>();
            setup?.Invoke(context);
            var map = context.Compile(_reader);
            return new DataRecordMappingEnumerable<T>(_reader, _context, map);
        }

        public object GetOutputParameterValue(string name)
        {
            TryTransitionToState(StateOutputParams);

            var parameterName = (name.StartsWith("@") ? name : "@" + name).ToLowerInvariant();

            var param = _command?.Parameters.Cast<DbParameter>()
                .Where(p => p.Direction != ParameterDirection.Input)
                .FirstOrDefault(p => p.ParameterName.ToLowerInvariant() == parameterName);
            if (param == null)
                return null;

            if (param.Value == DBNull.Value)
                return null;
            return param.Value;
        }

        public T GetOutputParameter<T>(string name)
        {
            var value = GetOutputParameterValue(name);
            if (value == null)
                return default(T);
            if (typeof(T) == typeof(object))
                return (T) value;
            if (value is T asT)
                return asT;
            if (value is IConvertible && typeof(IConvertible).IsAssignableFrom(typeof(T)))
                return (T) Convert.ChangeType(value, typeof(T));

            return default(T);
        }

        public T GetOutputParameterOrThrow<T>(string name)
        {
            var value = GetOutputParameterValue(name);
            if (value == null)
                return default(T);
            if (typeof(T) == typeof(object))
                return (T) value;
            if (value is T asT)
                return asT;
            if (typeof(T) == typeof(string))
                return (T)(object)value.ToString();
            if (value is IConvertible && typeof(IConvertible).IsAssignableFrom(typeof(T)))
                return (T) Convert.ChangeType(value, typeof(T));

            throw new Exception($"Cannot get output parameter '{name}' value. Expected type {typeof(T).FullName} but found {value.GetType().FullName} and no conversion can be found.");
        }

        public T GetOutputParameters<T>()
            where T : class, new()
        {
            // TODO: Can we introspect and get constructor parameters by name?
            var t = new T();
            if (_command == null)
                return t;

            var properties = typeof(T)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite)
                .Where(p => p.PropertyType == typeof(object) || typeof(IConvertible).IsAssignableFrom(p.PropertyType))
                .ToList();
            var getMethod = typeof(SqlDataReaderResults).GetMethod(nameof(GetOutputParameter));
            if (getMethod == null)
                throw new Exception($"Cannot find method {nameof(SqlDataReaderResults)}.{nameof(GetOutputParameter)}");
            foreach (var property in properties)
            {
                var getMethodTyped = getMethod.MakeGenericMethod(property.PropertyType);
                var value = getMethodTyped.Invoke(this, new object[] {property.Name});
                property.SetValue(t, value);
            }

            return t;
        }

        public IDataResults AdvanceToNextResultSet()
        {
            return AdvanceToResultSet(CurrentSet + 1);
        }

        public IDataResults AdvanceToResultSet(int num)
        {
            TryTransitionToState(StateReaderConsuming);

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
                if (!_reader.NextResult())
                    throw new Exception($"Could not read requested result set {num}. This stream only contains {CurrentSet} result sets.");
                CurrentSet++;
            }

            if (CurrentSet != num)
                throw new Exception($"Could not find result Set={num}. This should not happen.");

            return this;
        }

        public bool TryAdvanceToNextResultSet()
        {
            return TryAdvanceToResultSet(CurrentSet + 1);
        }

        public bool TryAdvanceToResultSet(int num)
        {
            TryTransitionToState(StateReaderConsuming);

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
                if (!_reader.NextResult())
                    return false;
                CurrentSet++;
            }

            if (CurrentSet != num)
                return false;

            return true;
        }

        protected void DisposeHeldReferences()
        {
            if (TryTransitionToState(StateDisposed))
            {
                _context?.MarkComplete();
                _reader.Dispose();
                _command?.Dispose();
                _context?.Dispose();
            }
        }

        private interface IResultState
        {
            string Name { get; }
            void Enter(IDataReader reader);
            void TryTransitionTo(string nextStateName);
        }

        private bool TryTransitionToState(string nextStateName)
        {
            _currentState.TryTransitionTo(nextStateName);
            if (_currentState.Name != nextStateName)
            {
                _currentState = _getState[nextStateName]();
                _currentState.Enter(_reader);
                return true;
            }

            return false;
        }

        private class InitialState : IResultState
        {
            public string Name => StateInitial;

            public void Enter(IDataReader reader)
            {
                throw new Exception("Cannot transition into the default state. Something is broken.");
            }

            public void TryTransitionTo(string nextStateName)
            {
            }
        }

        private class RawReaderConsumedState : IResultState
        {
            public string Name => StateRawReaderConsumed;
            public void Enter(IDataReader reader)
            {
                if (reader == null)
                    ThrowNullReaderException();
            }

            public void TryTransitionTo(string nextStateName)
            {
                switch (nextStateName)
                {
                    case StateRawReaderConsumed:
                        ThrowRawReaderConsumedException();
                        break;
                    case StateReaderConsuming:
                        ThrowRawReaderConsumedException();
                        break;
                    case StateOutputParams:
                        ThrowRawReaderConsumedException();
                        break;
                }
            }
        }

        private class ReaderConsumingState : IResultState
        {
            public string Name => StateReaderConsuming;
            public void Enter(IDataReader reader)
            {
                if (reader == null)
                    ThrowNullReaderException();
            }

            public void TryTransitionTo(string nextStateName)
            {
                switch (nextStateName)
                {
                    case StateRawReaderConsumed:
                        ThrowConsumeAfterStreamingStartedException();
                        break;
                }
            }
        }

        private class OutputParametersState : IResultState
        {
            public string Name => StateOutputParams;
            public void Enter(IDataReader reader)
            {
                if (reader != null && !reader.IsClosed)
                    reader.Close();
            }

            public void TryTransitionTo(string nextStateName)
            {
                switch (nextStateName)
                {
                    case StateRawReaderConsumed:
                        ThrowReaderClosedException();
                        break;
                    case StateReaderConsuming:
                        ThrowReaderClosedException();
                        break;
                }
            }
        }

        private class DisposedState : IResultState
        {
            public string Name => StateDisposed;
            public void Enter(IDataReader reader)
            {
            }

            public void TryTransitionTo(string nextStateName)
            {
                throw new ObjectDisposedException($"The {nameof(SqlDataReaderResults)} and underlying {nameof(IDataReader)} objects have been disposed and cannot be used for any other purposes.");
            }
        }

        private static void ThrowNullReaderException()
        {
            throw new NullReferenceException(
                $"This result does not contain a data reader. Readers are not produced when executing an {nameof(ISqlCommand)} variant. " +
                $"Depending on your command text, it may still be possible to read output parameters or connection metrics such as {nameof(IDataReader)}.{nameof(IDataReader.RecordsAffected)}");
        }

        private static void ThrowRawReaderConsumedException()
        {
            throw new InvalidOperationException(
                $"The raw {nameof(IDataReader)} has already been consumed and cannot be consumed again. " +
                $"Methods {nameof(IDataResults.AsRawReader)} and {nameof(IDataResults.AsRawReaderWithBetterErrorMessages)} relinquish ownership of the {nameof(IDataReader)}. " +
                $"The code component which holds a reference to the {nameof(IDataReader)} has full control over that object, including reading values and calling {nameof(IDisposable.Dispose)} when necessary.");
        }

        private static void ThrowConsumeAfterStreamingStartedException()
        {
            throw new InvalidOperationException(
                $"{nameof(IDataReader)} has already started streaming results. The raw reader cannot be consumed. " +
                $"{nameof(IDataReader)} can either be consumed raw (using methods {nameof(IDataResults.AsRawReader)} and {nameof(IDataResults.AsRawReaderWithBetterErrorMessages)}) " +
                $"or it can be consumed through this {nameof(IDataResults)} object (using methods like {nameof(IDataResults.AsEnumerable)} and {nameof(IDataResults.AdvanceToNextResultSet)}). " +
                "Once the reader has been consumed, it cannot be consumed again.");
        }

        private static void ThrowReaderClosedException()
        {
            throw new InvalidOperationException(
                $"The underlying {nameof(IDataReader)} has already been closed and cannot be consumed again. " +
                $"The {nameof(IDataReader)} object monopolizes the database connection for streaming results while it is open. " +
                $"The {nameof(IDataReader)} is closed to use the connection for other purposes, such as reading output parameters. " +
                $"In your application, make sure to read all result set data from the {nameof(IDataReader)} before attempting to read output parameters to avoid this issue.");
        }
    }

    public class SqlDataReaderResultsStream : SqlDataReaderResults, IDataResultsStream
    {
        public SqlDataReaderResultsStream(IDbCommand command, IExecutionContext context, IDataReader reader)
            : base(command, context, reader)
        {
        }

        // TODO: .AsRawReader() will return the reader which the user can .Dispose(), but not dispose the connection or command

        public void Dispose()
        {
            DisposeHeldReferences();
        }
    }
}