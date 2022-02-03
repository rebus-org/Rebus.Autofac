using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Autofac;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Retry.Simple;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;
#pragma warning disable 1998

namespace Rebus.Autofac.Tests.Bugs;

[TestFixture]
public class CanGetMessageHandlerWhenDoingSecondLevelRetries
{
    [Test]
    public async Task ItWorks()
    {
        var network = new InMemNetwork();
        var builder = new ContainerBuilder();
        var stuff = new ConcurrentQueue<string>();

        builder.RegisterInstance(stuff);

        builder.RegisterRebus(configure =>
            configure
                .Logging(l => l.None())
                .Transport(t => t.UseInMemoryTransport(network, "test"))
                .Options(o => o.SimpleRetryStrategy(secondLevelRetriesEnabled: true, maxDeliveryAttempts: 1))
        );

        builder.RegisterHandler<MyTestHandler>();

        await using var container = builder.Build();

        var bus = container.Resolve<IBus>();

        Console.WriteLine("Sending message");
        await bus.SendLocal("hej søtte!");

        Console.WriteLine("Waiting for message to arrive in queue 'done'...");
        await network.WaitForNextMessageFrom("done");
        Console.WriteLine("Got the message!");

        Assert.That(stuff.Count, Is.EqualTo(1), 
            "Expected second level handler to have been called only once!!");
    }

    class MyTestHandler : IHandleMessages<string>, IHandleMessages<IFailed<string>>
    {
        readonly ConcurrentQueue<string> _stuff;
        readonly IBus _bus;

        public MyTestHandler(ConcurrentQueue<string> stuff, IBus bus)
        {
            _stuff = stuff ?? throw new ArgumentNullException(nameof(stuff));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }

        public async Task Handle(IFailed<string> message)
        {
            Console.WriteLine("2nd level handler running");
            _stuff.Enqueue($"Handled failed message: {message.Message}");
            await _bus.Advanced.Routing.Send("done", "we're done now");
        }

        public async Task Handle(string message)
        {
            Console.WriteLine("Throwing an exception now!");
            throw new NotImplementedException("OH NOES");
        }
    }
}