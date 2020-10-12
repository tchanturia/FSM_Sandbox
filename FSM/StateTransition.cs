namespace FSM
{
    public class StateTransition<TState>
    {
        public StateTransition(TState newState, StateTransitionGuard guard)
        {
            NewState = newState;
            Guard = guard;
        }

        public StateTransitionGuard Guard { get; }
        public TState NewState { get; }
    }
}