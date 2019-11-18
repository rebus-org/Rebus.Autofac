using System.Threading.Tasks;
using Rebus.Handlers;

namespace MessageHandlers
{
    public class SecondHandler : IHandleMessages<string>
    {
        readonly EventAggregator _eventAggregator;

        public SecondHandler(EventAggregator eventAggregator) => _eventAggregator = eventAggregator;

        public Task Handle(string message)
        {
            _eventAggregator.Register($"SecondHandler handling {message}");
#if NET45
            return Task.FromResult(0);
#else
            return Task.CompletedTask;
#endif
        }
    }
}