using System;
using System.Collections.Generic;
using System.Data;
using System.Runtime.Serialization;
using CastIron.Sql.Execution;

namespace CastIron.Sql.Mapping
{
    /// <summary>
    /// Encapsulates both the IDataReader and the IDbCommand to give unified access to all result
    /// sets and output parameters from the command
    /// </summary>
    public class DataReaderResults : IDataResults
    {
        protected const string StateInitial = "";
        protected const string StateRawReaderConsumed = "RawReaderConsumed";
        protected const string StateReaderConsuming = "ReaderConsuming";
        protected const string StateOutputParams = "OutputParams";
        protected const string StateDisposed = "Disposed";

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
        private ParameterCache _parameterCache;

        public DataReaderResults(IDbCommand command, IExecutionContext context, IDataReader reader, int? rowsAffected = null)
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

        public ParameterCache GetParameters()
        {
            TryTransitionToState(StateOutputParams);
            if (_parameterCache == null)
                _parameterCache = new ParameterCache(_command);
            return _parameterCache;
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

        private void DisposeReferences(bool disposeReader)
        {
            // This method is only called from the streaming context. Otherwise the dependency 
            // lifecycles are managed elsewhere. When we're streaming, we have to manage everything
            // here. Unless we call .AsRawReader(), then we manage everything EXCEPT the reader,
            // which the user will need to manage manually.
            _context?.MarkComplete();
            if (disposeReader)
                _reader.Dispose();
            _command?.Dispose();
            _context?.Dispose();
        }

        private interface IResultState
        {
            string Name { get; }
            void Enter(IDataReader reader);
            void TryTransitionTo(string nextStateName, DataReaderResults results);
        }

        protected void TryTransitionToState(string nextStateName)
        {
            _currentState.TryTransitionTo(nextStateName, this);
            if (_currentState.Name == nextStateName)
                return;
            _currentState = _getState[nextStateName]();
            _currentState.Enter(_reader);
        }

        private class InitialState : IResultState
        {
            public string Name => StateInitial;

            public void Enter(IDataReader reader)
            {
                throw new Exception("Cannot transition into the default state. Something is broken.");
            }

            public void TryTransitionTo(string nextStateName, DataReaderResults results)
            {
                if (nextStateName == StateDisposed)
                    results.DisposeReferences(true);
            }
        }

        private class RawReaderConsumedState : IResultState
        {
            public string Name => StateRawReaderConsumed;
            public void Enter(IDataReader reader)
            {
                if (reader == null)
                    throw DataReaderException.ThrowNullReaderException();
            }

            public void TryTransitionTo(string nextStateName, DataReaderResults results)
            {
                switch (nextStateName)
                {
                    case StateRawReaderConsumed:
                    case StateReaderConsuming:
                    case StateOutputParams:
                        throw DataReaderException.ThrowRawReaderConsumedException();
                    case StateDisposed:
                        results.DisposeReferences(false);
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
                    throw DataReaderException.ThrowNullReaderException();
            }

            public void TryTransitionTo(string nextStateName, DataReaderResults results)
            {
                switch (nextStateName)
                {
                    case StateRawReaderConsumed:
                        throw DataReaderException.ThrowConsumeAfterStreamingStartedException();
                    case StateDisposed:
                        results.DisposeReferences(true);
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

            public void TryTransitionTo(string nextStateName, DataReaderResults results)
            {
                switch (nextStateName)
                {
                    case StateRawReaderConsumed:
                    case StateReaderConsuming:
                        throw DataReaderException.ThrowReaderClosedException();
                    case StateDisposed:
                        results.DisposeReferences(true);
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

            public void TryTransitionTo(string nextStateName, DataReaderResults results)
            {
                if (nextStateName != StateDisposed)
                {
                    throw new ObjectDisposedException(
                        $"The {nameof(DataReaderResults)} and underlying {nameof(IDataReader)} objects " +
                        "have been disposed and cannot be used for any other purposes.");
                }
            }
        }
    }

    public class DataReaderResultsStream : DataReaderResults, IDataResultsStream
    {
        public DataReaderResultsStream(IDbCommand command, IExecutionContext context, IDataReader reader)
            : base(command, context, reader)
        {
        }

        // TODO: .AsRawReader() will return the reader which the user can .Dispose(), but not dispose the connection or command

        public void Dispose()
        {
            TryTransitionToState(StateDisposed);
        }
    }

    [Serializable]
    public class DataReaderException : Exception
    {
        public DataReaderException()
        {
        }

        public DataReaderException(string message) : base(message)
        {
        }

        public DataReaderException(string message, Exception inner) : base(message, inner)
        {
        }

        protected DataReaderException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }

        public static DataReaderException ThrowNullReaderException()
        {
            return new DataReaderException(
                $"This result does not contain a data reader. Readers are not produced when executing an {nameof(ISqlCommand)} variant. " +
                $"Depending on your command text, it may still be possible to read output parameters or connection metrics such as {nameof(IDataReader)}.{nameof(IDataReader.RecordsAffected)}");
        }

        public static DataReaderException ThrowRawReaderConsumedException()
        {
            return new DataReaderException(
                $"The raw {nameof(IDataReader)} has already been consumed and cannot be consumed again. " +
                $"The method {nameof(IDataResults.AsRawReader)} relinquishes ownership of the {nameof(IDataReader)}. " +
                $"The code component which holds a reference to the {nameof(IDataReader)} has full control over that object, including reading values and calling {nameof(IDisposable.Dispose)} when necessary.");
        }

        public static DataReaderException ThrowConsumeAfterStreamingStartedException()
        {
            return new DataReaderException(
                $"{nameof(IDataReader)} has already started streaming results. The raw reader cannot be consumed. " +
                $"{nameof(IDataReader)} can either be consumed raw (using .{nameof(IDataResults.AsRawReader)}()) " +
                $"or it can be consumed through this {nameof(IDataResults)} object (using .{nameof(IDataResults.AsEnumerable)}()). " +
                "Once the reader has been consumed, it cannot be consumed again.");
        }

        public static DataReaderException ThrowReaderClosedException()
        {
            return new DataReaderException(
                $"The underlying {nameof(IDataReader)} has already been closed and cannot be consumed again. " +
                $"The {nameof(IDataReader)} object monopolizes the database connection for streaming results while it is open. " +
                $"The {nameof(IDataReader)} is closed to use the connection for other purposes, such as reading output parameters. " +
                $"In your application, make sure to read all result set data from the {nameof(IDataReader)} before attempting to read output parameters to avoid this issue.");
        }
    }
}