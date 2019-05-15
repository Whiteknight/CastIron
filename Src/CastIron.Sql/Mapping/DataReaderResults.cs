using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.Serialization;
using CastIron.Sql.Execution;

namespace CastIron.Sql.Mapping
{
    public class StringKeyedStateMachine
    {
        protected const string StateInitial = "";
        private readonly Dictionary<string, State> _states;
        private State _currentState;

        public StringKeyedStateMachine()
        {
            _states = new Dictionary<string, State>();
            _currentState = null;
        }

        public class StateBuilder
        {
            private readonly State _state;

            public StateBuilder(State state)
            {
                _state = state;
            }

            public StateBuilder TransitionOnEvent(string key, string nextKey, Action onTransition = null)
            {
                _state.TransitionOnEvent(key, nextKey, onTransition);
                return this;
            }
        }

        public class State
        {
            public string Name { get; }
            private readonly Dictionary<string, Transition> _transitions;

            public State(string name)
            {
                Name = name;
                _transitions = new Dictionary<string, Transition>();
            }

            public void TransitionOnEvent(string key, string nextKey, Action onTransition)
            {
                var transition = new Transition(nextKey, onTransition);
                _transitions.Add(key, transition);
            }

            public Transition GetTransitionForKey(string key)
            {
                return _transitions.ContainsKey(key) ? _transitions[key] : null;
            }
        }

        public class Transition
        {
            public Transition(string newKey, Action onTransition)
            {
                NewKey = newKey;
                OnTransition = onTransition;
            }

            public string NewKey { get; }
            public Action OnTransition { get; }
        }

        public StateBuilder AddState(string name)
        {
            var state = new State(name);
            _states.Add(name, state);
            var builder = new StateBuilder(state);
            return builder;
        }

        public StateBuilder UpdateState(string name)
        {
            var state = _states.Values.FirstOrDefault(s => s.Name == name);
            return new StateBuilder(state);
        }

        public void ReceiveEvent(string key)
        {
            if (_currentState == null)
            {
                _currentState = _states[key];
                return;
            }

            var transition = _currentState.GetTransitionForKey(key);
            if (transition == null)
                throw new Exception($"Cannot transition from state {_currentState?.Name ?? "initial"} on key {key}");

            transition.OnTransition?.Invoke();

            if (transition.NewKey == null || transition.NewKey == StateInitial)
                throw new Exception($"Cannot transition to the initial state from state {_currentState.Name} on key {key}");
            _currentState = _states[transition.NewKey];
        }
    }

    public abstract class DataReaderResultsBase : IDataResultsBase
    {
        
        protected const string StateRawReaderConsumed = "RawReaderConsumed";
        protected const string StateReaderConsuming = "ReaderConsuming";
        protected const string StateOutputParams = "OutputParams";

        protected StringKeyedStateMachine StateMachine { get; }

        protected IDbCommand Command { get; }
        protected IExecutionContext Context { get; }
        protected IDataReader Reader { get;  }

        private ParameterCache _parameterCache;

        protected DataReaderResultsBase(IDbCommand command, IExecutionContext context, IDataReader reader, int? rowsAffected)
        {
            Command = command;
            Context = context;
            Reader = reader;
            CurrentSet = 0;
            RowsAffected = rowsAffected ?? reader?.RecordsAffected ?? 0;
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
        public int RowsAffected { get; }

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