using System;
using System.Collections.Generic;
using System.Linq;

namespace FSM
{
    public class StateMachineBuilder<TState> where TState : Enum
    {
        private readonly Dictionary<TState, StateConfiguration> _states =
            new Dictionary<TState, StateConfiguration>();

        public StateDefinitionBuilder InState(params TState[] states) =>
            new StateDefinitionBuilder(this, states);

        public StateDefinitionBuilder InAllStatesExcept(params TState[] states) =>
            new StateDefinitionBuilder(
                this,
                Enum.GetValues(typeof(TState))
                    .Cast<TState>()
                    .Where(s => !states.Contains(s))
                    .ToArray()
            );

        public StateDefinitionBuilder InAllStates() =>
            new StateDefinitionBuilder(
                this,
                Enum.GetValues(typeof(TState))
                    .Cast<TState>()
                    .ToArray()
            );

        private void AddState(TState state, StateConfiguration configuration)
        {
            if (!_states.TryGetValue(state, out var existingConfiguration))
            {
                existingConfiguration = new StateConfiguration();
                _states[state] = existingConfiguration;
            }

            foreach (var messageHandler in configuration.CommandHandlers)
            {
                existingConfiguration.CommandHandlers.Add(messageHandler.Key, messageHandler.Value);
            }

            foreach (var stateTransitions in configuration.StateTransitions)
            {
                if (!existingConfiguration.StateTransitions.TryGetValue(
                    stateTransitions.Key,
                    out var existingTransitions
                ))
                {
                    existingTransitions = new List<StateTransition<TState>>();
                    existingConfiguration.StateTransitions[stateTransitions.Key] = existingTransitions;
                }

                foreach (var stateTransition in stateTransitions.Value)
                {
                    existingTransitions.Add(stateTransition);
                }
            }

            existingConfiguration.EnterAction = configuration.EnterAction ?? existingConfiguration.EnterAction;
            existingConfiguration.ExitAction = configuration.ExitAction ?? existingConfiguration.ExitAction;
        }

        public StateMachine<TState> Build(TState initialState)
        {
            var states = _states.ToDictionary(
                s => s.Key,
                s =>
                    BuildStateDefinition(s.Key, s.Value)
            );

            return new StateMachine<TState>(states, initialState);
        }

        private StateDefinition<TState> BuildStateDefinition(
            TState state,
            StateConfiguration stateConfiguration
        )
        {
            var commandHandlers = new ICommandHandler[MessageTypeIdProvider.MaxId];

            foreach (var commandHandler in stateConfiguration.CommandHandlers)
            {
                commandHandlers[commandHandler.Key] = commandHandler.Value;
            }

            var stateTransitions = new StateTransition<TState>[MessageTypeIdProvider.MaxId][];

            foreach (var stateTransition in stateConfiguration.StateTransitions)
            {
                stateTransitions[stateTransition.Key] = stateTransition.Value.ToArray();
            }

            return new StateDefinition<TState>(
                state,
                commandHandlers,
                stateTransitions,
                stateConfiguration.EnterAction,
                stateConfiguration.ExitAction
            );
        }

        public class StateDefinitionBuilder
        {
            private readonly StateMachineBuilder<TState> _stateMachineBuilder;
            private readonly TState[] _states;

            private readonly StateConfiguration _configuration =
                new StateConfiguration();

            public StateDefinitionBuilder(StateMachineBuilder<TState> stateMachineBuilder, TState[] states)
            {
                _stateMachineBuilder = stateMachineBuilder;
                _states = states;
            }

            public StateDefinitionBuilder OnEnter(Action action)
            {
                _configuration.EnterAction = action;
                
                return this;
            }

            public StateDefinitionBuilder OnExit(Action action)
            {
                _configuration.ExitAction = action;
                
                return this;
            }

            public StateCommandHandlerBuilder<TCommand> When<TCommand>() =>
                new StateCommandHandlerBuilder<TCommand>(this);

            public StateDefinitionBuilder InState(params TState[] states)
            {
                foreach (var state in _states)
                    _stateMachineBuilder.AddState(state, _configuration);

                return _stateMachineBuilder.InState(states);
            }

            public StateDefinitionBuilder InAllStatesExcept(params TState[] states)
            {
                foreach (var state in _states)
                    _stateMachineBuilder.AddState(state, _configuration);

                return _stateMachineBuilder.InAllStatesExcept(states);
            }

            public StateDefinitionBuilder InAllStates()
            {
                foreach (var state in _states)
                    _stateMachineBuilder.AddState(state, _configuration);

                return _stateMachineBuilder.InAllStates();
            }

            public StateMachine<TState> Build(TState initialState)
            {
                foreach (var state in _states)
                    _stateMachineBuilder.AddState(state, _configuration);

                return _stateMachineBuilder.Build(initialState);
            }

            public void AddTransition<TCommand>(TState newState) =>
                AddTransition<TCommand>(newState, () => true);

            public void AddTransition<TCommand>(TState newState, Func<bool> condition)
            {
                var commandTypeId = CommandTypeId<TCommand>.Value;
                
                var stateTransition = new StateTransition<TState>(newState, new StateTransitionGuard(condition));

                if (!_configuration.StateTransitions.TryGetValue(commandTypeId, out var stateTransitions))
                {
                    _configuration.StateTransitions[commandTypeId] = new List<StateTransition<TState>>
                    {
                        stateTransition
                    };
                }
                else
                {
                    stateTransitions.Add(stateTransition);
                }
            }
            
            private void AddHandler<TCommand>(Action handler) =>
                _configuration.CommandHandlers.Add(
                    CommandTypeId<TCommand>.Value,
                    new CommandHandler<TCommand>((in TCommand msg) => handler())
                );

            private void AddHandler<TCommand>(ActionIn<TCommand> handler) =>
                _configuration.CommandHandlers.Add(
                    CommandTypeId<TCommand>.Value,
                    new CommandHandler<TCommand>(handler)
                );
            
            public class StateCommandHandlerBuilder<TCommand>
            {
                private readonly StateDefinitionBuilder _stateDefinitionBuilder;

                public StateCommandHandlerBuilder(StateDefinitionBuilder stateDefinitionBuilder)
                {
                    _stateDefinitionBuilder = stateDefinitionBuilder;
                }
                public StateDefinitionBuilder Do(Action handle)
                {
                    _stateDefinitionBuilder.AddHandler<TCommand>(handle);

                    return _stateDefinitionBuilder;
                }
                public StateDefinitionBuilder Do(ActionIn<TCommand> handle)
                {
                    _stateDefinitionBuilder.AddHandler(handle);

                    return _stateDefinitionBuilder;
                }

                public StateDefinitionBuilder TransitTo(TState state)
                {
                    _stateDefinitionBuilder.AddTransition<TCommand>(state);

                    return _stateDefinitionBuilder;
                }
                
                public StateDefinitionBuilder TransitTo(TState state, Func<bool> canTransit)
                {
                    _stateDefinitionBuilder.AddTransition<TCommand>(state, canTransit);

                    return _stateDefinitionBuilder;
                }
            }
        }

        private sealed class StateConfiguration
        {
            public Dictionary<int, ICommandHandler> CommandHandlers { get; } =
                new Dictionary<int, ICommandHandler>();

            public Dictionary<int, List<StateTransition<TState>>> StateTransitions { get; } =
                new Dictionary<int, List<StateTransition<TState>>>();

            public Action EnterAction { get; set; }

            public Action ExitAction { get; set; }
        }
    }
}