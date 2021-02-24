using System.Threading.Tasks;
using Rebus.Handlers;
#pragma warning disable 1998

namespace MessageHandlers
{
    public class FirstHandler : IHandleMessages<string>
    {
        readonly EventAggregator _eventAggregator;

        public FirstHandler(EventAggregator eventAggregator) => _eventAggregator = eventAggregator;

        public async Task Handle(string message) => _eventAggregator.Register($"FirstHandler handling {message}");
    }
}