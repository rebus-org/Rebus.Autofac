using System.Threading.Tasks;
using Rebus.Handlers;
#pragma warning disable 1998

namespace MessageHandlers.SecondHandlerQueue
{
    public class ThirdHandler : IHandleMessages<SecondMessage>
    {
        readonly EventAggregator _eventAggregator;

        public ThirdHandler(EventAggregator eventAggregator) => _eventAggregator = eventAggregator;

        public async Task Handle(SecondMessage message) => _eventAggregator.Register($"ThirdHandler handling {message.Message}");
    }
}