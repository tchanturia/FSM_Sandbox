using System;

namespace FSM
{
    public class StateTransitionGuard
    {
        private readonly Func<bool> _condition;

        public StateTransitionGuard(Func<bool> condition)
        {
            _condition = condition;
        }

        public bool IsConditionMet => _condition();
    }
}