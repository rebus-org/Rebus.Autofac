using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Rebus.Handlers;
#pragma warning disable 1998

namespace AdditionalHandlerAssembly
{
    public class ControllerChangeMessageHandler : IHandleMessages<ControllerChangeMessage>
    {
        readonly ConcurrentQueue<ControllerChangeMessage> _handledMessages;

        public ControllerChangeMessageHandler(ConcurrentQueue<ControllerChangeMessage> handledMessages) => _handledMessages = handledMessages ?? throw new ArgumentNullException(nameof(handledMessages));

        public async Task Handle(ControllerChangeMessage message) => _handledMessages.Enqueue(message);
    }

    public class ControllerChangeMessage { }
}
