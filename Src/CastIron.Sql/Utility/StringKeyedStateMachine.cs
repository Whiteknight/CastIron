using System;
using System.Collections.Generic;
using System.Linq;

namespace CastIron.Sql.Utility
{
    // Simple state machine implementation
    // Warning, this is intended primarily for internal use and doesn't include some of the safety
    // features that a more robust and general-purpose implementation would have.
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
                throw new InvalidOperationException($"Cannot transition from state {_currentState?.Name ?? "initial"} on key {key}");
            if (transition.NewKey == null || transition.NewKey == StateInitial)
                throw new InvalidOperationException($"Cannot transition to the initial state from state {_currentState.Name} on key {key}");

            transition.OnTransition?.Invoke();
            _currentState = _states[transition.NewKey];
        }
    }
}