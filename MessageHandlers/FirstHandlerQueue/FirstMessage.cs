namespace MessageHandlers.FirstHandlerQueue
{
    public class FirstMessage : FirstQueueMessageBase
    {
        public FirstMessage(string message)
        {
            Message = message;
        }
        public string Message;
    }
}