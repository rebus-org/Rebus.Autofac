namespace MessageHandlers.SecondHandlerQueue
{
    public class SecondMessage : SecondMessageQueueBase
    {
        public SecondMessage(string message)
        {
            Message = message;
        }
        public string Message;
    }
}