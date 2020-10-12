namespace FSM
{
    public delegate void ActionIn<T>(in T msg);
   
    public interface ICommandHandler
    {
        
    }

    public class CommandHandler<T> : ICommandHandler
    {
        private readonly ActionIn<T> _handler;

        public CommandHandler(ActionIn<T> handler)
        {
            _handler = handler;
        }

        public void Handle(in T msg) => _handler(in msg);
    }
}