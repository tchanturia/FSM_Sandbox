using System;
using System.Collections.Generic;

namespace FSM
{
    public sealed class StateMachine<TState>
        where TState : Enum
    {
        private readonly IReadOnlyDictionary<TState, StateDefinition<TState>> _states;

        private StateDefinition<TState> _currentState;

        public StateMachine(
            IReadOnlyDictionary<TState, StateDefinition<TState>> states,
            TState initialState
        )
        {
            _states = states;
            _currentState = _states[initialState];
            _currentState.Enter();
        }

        public TState CurrentState => _currentState.State;
        
        public void Handle<TCommand>(in TCommand message)
        {
            if (_currentState.TryGetTransition<TCommand>(out var stateTransition))
                TransitTo(stateTransition.NewState);

            _currentState.Handle(message);
        }

        public void TransitTo(TState state)
        {
            _currentState.Exit();
            _currentState = _states[state];
            _currentState.Enter();
        }
    }
}