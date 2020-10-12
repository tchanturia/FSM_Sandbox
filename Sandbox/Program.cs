using System;
using FSM;

namespace Sandbox
{
    class Program
    {
        static void Main(string[] args)
        {
            var stateMachine = new EventFeedStateMachine(threshold: 3);

            /* Current state: Normal */
            PrintState(stateMachine); 

            Console.WriteLine();
            
            /*
            Exception occurred
            Exceptions received: 1
            Current state: Exception
            */
            PrintExceptionOccurred();
            stateMachine.ExceptionOccurred(new Exception());
            PrintExceptionsReceived(stateMachine);
            PrintState(stateMachine);

            Console.WriteLine();

            /*
            Exception occurred
            Exceptions received: 2
            Current state: Exception
             */
            PrintExceptionOccurred();
            stateMachine.ExceptionOccurred(new Exception());
            PrintExceptionsReceived(stateMachine);
            PrintState(stateMachine);

            Console.WriteLine();
            
            /*
            Exception occurred
            Exceptions received: 2
            Current state: Exception
            */
            PrintExceptionOccurred();
            stateMachine.ExceptionOccurred(new Exception());
            PrintExceptionsReceived(stateMachine);
            PrintState(stateMachine);
            
            Console.WriteLine();
            
            /*
            Successful dispatch
            Current state: Normal
            */
            PrintSuccessfulDispatch();
            stateMachine.SuccessfulDispatch();
            PrintState(stateMachine);
            
            Console.WriteLine();
            
            /*
            Exception occurred
            Exceptions received: 1
            Current state: Exception
            */
            PrintExceptionOccurred();
            stateMachine.ExceptionOccurred(new Exception());
            PrintExceptionsReceived(stateMachine);
            PrintState(stateMachine);

            Console.WriteLine();
            
            /*
            Successful dispatch
            Current state: Normal
            */
            PrintSuccessfulDispatch();
            stateMachine.SuccessfulDispatch();
            PrintState(stateMachine);

            Console.ReadLine();
        }


        static void PrintExceptionOccurred() => Console.WriteLine("Exception occurred");

        static void PrintSuccessfulDispatch() => Console.WriteLine("Successful dispatch");
        
        static void PrintState(EventFeedStateMachine stateMachine) =>
            Console.WriteLine($"Current state: {stateMachine.State.ToString()}");

        static void PrintExceptionsReceived(EventFeedStateMachine stateMachine) =>
            Console.WriteLine($"Exceptions received: {stateMachine.ExceptionsReceived}");
    }

    public enum EventFeedState
    {
        Normal = 0,
        Exception = 1,
        HalfOpen = 2
    }

    public static class Commands
    {
        public readonly struct ExceptionOccurred
        {
            public readonly Exception Exception;

            public ExceptionOccurred(Exception exception)
            {
                Exception = exception;
            }
        }

        public readonly struct SuccessfulDispatch
        {
            
        }
    }

    public class EventFeedStateMachine
    {
        private readonly StateMachine<EventFeedState> _stateMachine;

        public EventFeedStateMachine(int threshold)
        {
            Threshold = threshold;

            _stateMachine = CreateStateMachine();
        }

        public EventFeedState State => _stateMachine.CurrentState;
        public int Threshold { get; }
        public int ExceptionsReceived { get; private set; }

        public void ExceptionOccurred(Exception ex) => _stateMachine.Handle(new Commands.ExceptionOccurred(ex));
        public void SuccessfulDispatch() => _stateMachine.Handle(new Commands.SuccessfulDispatch());
        
        private void ExceptionStateOnEnter() => ExceptionsReceived = 1;

        private bool IsThresholdReached() => ++ExceptionsReceived == Threshold;

        private StateMachine<EventFeedState> CreateStateMachine() =>
            new StateMachineBuilder<EventFeedState>()

                .InState(EventFeedState.Normal)
                .When<Commands.ExceptionOccurred>().TransitTo(EventFeedState.Exception)

                .InState(EventFeedState.Exception)
                .OnEnter(ExceptionStateOnEnter)
                .When<Commands.ExceptionOccurred>().TransitTo(EventFeedState.HalfOpen, canTransit: IsThresholdReached)

                .InState(EventFeedState.HalfOpen, EventFeedState.Exception)
                .When<Commands.SuccessfulDispatch>().TransitTo(EventFeedState.Normal)
                
                .Build(initialState: EventFeedState.Normal);
    }
}