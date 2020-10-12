namespace FSM
{
    public static class MessageTypeIdProvider
    {
        public static int MaxId { get; private set; }

        public static int GetNext() => MaxId++;
    }

    public static class CommandTypeId<T>
    {
        public static readonly int Value;

        static CommandTypeId() => Value = MessageTypeIdProvider.GetNext();
    }
}