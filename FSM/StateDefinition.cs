using System;

namespace FSM
{
    public class StateDefinition<TState>
    {
        private readonly ICommandHandler[] _commandHandlers;
        private readonly StateTransition<TState>[][] _transitions;
        private readonly Action _enterAction;
        private readonly Action _exitAction;

        public readonly TState State;

        public StateDefinition(
            TState state,
            ICommandHandler[] commandHandlers,
            StateTransition<TState>[][] transitions,
            Action enterAction,
            Action exitAction
        )
        {
            _commandHandlers = commandHandlers;
            _transitions = transitions;
            _enterAction = enterAction;
            _exitAction = exitAction;
            State = state;
        }

        public void Enter() => _enterAction?.Invoke();

        public void Exit() => _exitAction?.Invoke();

        public bool TryGetTransition<T>(out StateTransition<TState> stateTransition)
        {
            var commandTypeId = CommandTypeId<T>.Value;

            if (_transitions.Length <= commandTypeId)
            {
                stateTransition = null;
                return false;
            }
            
            var transitions = _transitions[commandTypeId];

            if (transitions != null)
            {
                for (var i = 0; i < transitions.Length; i++)
                {
                    var transition = transitions[i];

                    if (transition.Guard.IsConditionMet)
                    {
                        stateTransition = transition;
                        return true;
                    }
                }
            }

            stateTransition = null;
            return false;
        }

        public void Handle<TCommand>(in TCommand command)
        {
            var commandTypeId = CommandTypeId<TCommand>.Value;
            
            if (_commandHandlers.Length <= commandTypeId) return;

            var commandHandler = _commandHandlers[commandTypeId];

            ((CommandHandler<TCommand>) commandHandler)?.Handle(in command);
        }
    }
}