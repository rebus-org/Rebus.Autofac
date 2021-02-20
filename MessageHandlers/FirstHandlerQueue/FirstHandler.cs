using System.Threading.Tasks;
using Rebus.Handlers;
#pragma warning disable 1998

namespace MessageHandlers.FirstHandlerQueue
{
    public class FirstHandler : IHandleMessages<FirstMessage>
    {
        readonly EventAggregator _eventAggregator;

        public FirstHandler(EventAggregator eventAggregator) => _eventAggregator = eventAggregator;

        public async Task Handle(FirstMessage message) => _eventAggregator.Register($"FirstHandler handling {message.Message}");
    }
}