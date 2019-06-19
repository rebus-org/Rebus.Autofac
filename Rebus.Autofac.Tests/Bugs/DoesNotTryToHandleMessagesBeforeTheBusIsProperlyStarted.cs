using System.Linq;
using System.Threading.Tasks;
using Autofac;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Logging;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Tests.Contracts.Utilities;
using Rebus.Transport.InMem;
// ReSharper disable RedundantArgumentDefaultValue
// ReSharper disable ClassNeverInstantiated.Local
#pragma warning disable 1998

namespace Rebus.Autofac.Tests.Bugs
{
    [TestFixture]
    public class DoesNotTryToHandleMessagesBeforeTheBusIsProperlyStarted : FixtureBase
    {
        [Test]
        [Repeat(10)]
        public async Task ItWorks()
        {
            const string queueName = "testappqueue";
            const int numberOfMessages = 10;

            var network = new InMemNetwork();
            var listLoggerFactory = new ListLoggerFactory(detailed: true);
            var sharedCounter = new SharedCounter(numberOfMessages);

            // deliver a message for our endpoint
            network.CreateQueue(queueName);
            
            var client = GetOneWayClient(network, listLoggerFactory);
            
            numberOfMessages.Times(() => client.Advanced.Routing.Send(queueName, "HEJ MED DIG MIN VEN").Wait());

            // prepare our endpoint
            var builder = new ContainerBuilder();

            builder.RegisterRebus((configurer, context) => configurer
                .Logging(l => l.Use(listLoggerFactory))
                .Transport(t => t.UseInMemoryTransport(network, queueName))
                .Options(o =>
                {
                    o.SetNumberOfWorkers(1);
                    o.SetMaxParallelism(30);
                }));

            builder.RegisterHandler<MyMessageHandler>();

            builder.RegisterInstance(sharedCounter);

            // start it
            using (builder.Build())
            {
                sharedCounter.WaitForResetEvent(timeoutSeconds: 5);
            }

            // ensure no exceptions occurred

            Assert.That(listLoggerFactory.Count(log => log.Level >= LogLevel.Warn), Is.EqualTo(0),
                "Expected exactly ZERO warnings");
        }

        IBus GetOneWayClient(InMemNetwork network, ListLoggerFactory listLoggerFactory)
        {
            var activator = new BuiltinHandlerActivator();

            Using(activator);

            return Configure.With(activator)
                .Logging(l => l.Use(listLoggerFactory))
                .Transport(t => t.UseInMemoryTransportAsOneWayClient(network))
                .Start();
        }

        class MyMessageHandler : IHandleMessages<string>
        {
            readonly SharedCounter _sharedCounter;

            public MyMessageHandler(SharedCounter sharedCounter) => _sharedCounter = sharedCounter;

            public async Task Handle(string message) => _sharedCounter.Decrement();
        }
    }
}