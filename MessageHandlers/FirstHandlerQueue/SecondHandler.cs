using System.Threading.Tasks;
using Rebus.Handlers;
#pragma warning disable 1998

namespace MessageHandlers.FirstHandlerQueue
{
    public class SecondHandler : IHandleMessages<FirstMessage>
    {
        readonly EventAggregator _eventAggregator;

        public SecondHandler(EventAggregator eventAggregator) => _eventAggregator = eventAggregator;

        public async Task Handle(FirstMessage message) => _eventAggregator.Register($"SecondHandler handling {message.Message}");
    }
}