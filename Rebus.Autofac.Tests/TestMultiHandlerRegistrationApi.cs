using System;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Autofac.Core;
using MessageHandlers;
using MessageHandlers.FirstHandlerQueue;
using MessageHandlers.SecondHandlerQueue;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Routing.TypeBased;
using Rebus.Transport.InMem;
#pragma warning disable 1998
// ReSharper disable RedundantArgumentDefaultValue
// ReSharper disable ArgumentsStyleNamedExpression

namespace Rebus.Autofac.Tests
{
    [TestFixture]
    public class TestMultiHandlerRegistrationApi
    {
        [Test]
        public void RegisterOneWayRebusThrowsIfNotOneWayBus()
        {
            var builder = new ContainerBuilder();

            builder.RegisterOneWayRebus(configure => configure
                .Logging(l => l.Console(minLevel: LogLevel.Debug))
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "ioc-test")));

            Assert.Throws<DependencyResolutionException>(() =>
            {
                using (var container = builder.Build())
                {
                    // Resolve the IBus, which should throw and error if it's not one way
                    container.Resolve<IBus>();
                }
            });
        }

        [Test]
        public void RegisterOneWayRebusThrowsWhenAddingTwice()
        {
            var builder = new ContainerBuilder();

            builder.RegisterOneWayRebus(configure => configure
                .Logging(l => l.Console(minLevel: LogLevel.Debug))
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "ioc-test")));

            builder.RegisterOneWayRebus(configure => configure
                .Logging(l => l.Console(minLevel: LogLevel.Debug))
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "ioc-test")));

            Assert.Throws<DependencyResolutionException>(() =>
            {
                builder.Build();
            });
        }

        private IContainer _container;

        [Test]
        public async Task RealMultipleBuses()
        {
            // We need a common in memory network for this test
            var inMemNetwork = new InMemNetwork();
            const string firstQueueName = "first-queue";
            const string secondQueueName = "second-queue";

            // Set up a single instance of the event aggregator and register it
            var builder = new ContainerBuilder();
            var eventAggregator = new EventAggregator();
            builder.RegisterInstance(eventAggregator).SingleInstance();

            // Configure handlers for receiving messages in the first queue and start it up
            builder.RegisterRebusMultipleHandlers<FirstQueueMessageBase>(
                configurer => configurer
                    .Transport(t => t.UseInMemoryTransport(inMemNetwork, firstQueueName))
                    .Options(o =>
                    {
                        o.SetNumberOfWorkers(1);
                        o.SetMaxParallelism(1);
                    }));
            builder.RegisterHandlersFromAssemblyNamespaceOf<FirstQueueMessageBase>();

            // Configure container for receiving messages in the second queue and start it up
            builder.RegisterRebusMultipleHandlers<SecondMessageQueueBase>(
                configurer => configurer
                    .Transport(t => t.UseInMemoryTransport(inMemNetwork, secondQueueName))
                    .Options(o =>
                    {
                        o.SetNumberOfWorkers(1);
                        o.SetMaxParallelism(1);
                    }));
            builder.RegisterHandlersFromAssemblyNamespaceOf<SecondMessageQueueBase>();

            // Now configure the bus for sending messages. This will have no worker threads and it's configured
            // to send messages to the two separate queues separated by namespace.
            builder.RegisterOneWayRebus(
                configurer => configurer
                    .Transport(t => t.UseInMemoryTransportAsOneWayClient(inMemNetwork))
                    .Routing(r => r.TypeBased()
                        .MapAssemblyDerivedFrom<FirstQueueMessageBase>(firstQueueName)
                        .MapAssemblyDerivedFrom<SecondMessageQueueBase>(secondQueueName))
            );

            // Build the container. No busses have started up yet, as we need to start them each
            await using (_container = builder.Build())
            {
                // Start up the event handlers
                var firstBusStarter = _container.Resolve<IBusStarter<FirstQueueMessageBase>>();
                firstBusStarter.Start();
                var secondBusStarter = _container.Resolve<IBusStarter<SecondMessageQueueBase>>();
                secondBusStarter.Start();

                // Now resolve the bus we can send messages with
                var bus = _container.Resolve<IBus>();
                await bus.Send(new FirstMessage("HI THERE"));
                await bus.Send(new FirstMessage("HOW ARE YOU?"));
                await bus.Send(new SecondMessage("HI THERE"));
                await bus.Send(new SecondMessage("HOW ARE YOU?"));

                // Wait for the messages to get received in the handler threads
                await Task.Delay(500);

                // Check it all worked
                var events = _container.Resolve<EventAggregator>().OrderBy(s => s).ToList();
                Assert.That(events.Count, Is.EqualTo(6));
                Assert.AreEqual(events[0], "FirstHandler handling HI THERE");
                Assert.AreEqual(events[1], "FirstHandler handling HOW ARE YOU?");
                Assert.AreEqual(events[2], "SecondHandler handling HI THERE");
                Assert.AreEqual(events[3], "SecondHandler handling HOW ARE YOU?");
                Assert.AreEqual(events[4], "ThirdHandler handling HI THERE");
                Assert.AreEqual(events[5], "ThirdHandler handling HOW ARE YOU?");
                Console.WriteLine(string.Join(Environment.NewLine, events));
            }
        }
    }
}