using System;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using MessageHandlers;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Routing.TypeBased;
using Rebus.Transport.InMem;

namespace Rebus.Autofac.Tests
{
    [TestFixture]
    public class TestMultipleContainerBuilders
    {
        [Test]
        public async Task MultipleRealBusesAndSeparateTransports()
        {
            // We need a common in memory network for this test
            var inMemNetwork = new InMemNetwork();
            const string firstQueueName = "first-queue";
            const string secondQueueName = "second-queue";

            // Set up a single instance of the event aggregator
            var eventAggregator = new EventAggregator();

            // Configure container for receiving the first message type and start it up
            var firstBuilder = new ContainerBuilder();
            firstBuilder.RegisterHandler<FirstHandler>();
            firstBuilder.RegisterInstance(eventAggregator).SingleInstance();
            firstBuilder.RegisterRebus(
                configurer => configurer
                    .Transport(t => t.UseInMemoryTransport(inMemNetwork, firstQueueName))
                    .Options(o =>
                    {
                        o.SetNumberOfWorkers(1);
                        o.SetMaxParallelism(1);
                    })
            );
            using var firstContainer = firstBuilder.Build();

            // Configure container for receiving the second message type and start it up
            var secondBuilder = new ContainerBuilder();
            secondBuilder.RegisterHandler<SecondHandler>();
            secondBuilder.RegisterInstance(eventAggregator).SingleInstance();
            secondBuilder.RegisterRebus(
                configurer => configurer
                    .Transport(t => t.UseInMemoryTransport(inMemNetwork, secondQueueName))
                    .Options(o =>
                    {
                        o.SetNumberOfWorkers(1);
                        o.SetMaxParallelism(1);
                    })
            );
            using var secondContainer = secondBuilder.Build();

            // Now configure the bus for sending messages. This will have no worker threads.
            var sendBuilder = new ContainerBuilder();
            sendBuilder.RegisterRebus(
                configurer => configurer
                    .Transport(t => t.UseInMemoryTransportAsOneWayClient(inMemNetwork))
                    .Routing(r => r.TypeBased()
                        .Map<FirstMessage>(firstQueueName)
                        .Map<SecondMessage>(secondQueueName))
            );

            using var sendContainer = sendBuilder.Build();
            var bus = sendContainer.Resolve<IBus>();

            await bus.Send(new FirstMessage("first message"));
            await bus.Send(new SecondMessage("second message"));

            await Task.Delay(500);

            var events = eventAggregator.ToList();

            Assert.That(events.Count, Is.EqualTo(2));
            Assert.AreEqual(true, events.Contains("FirstHandler handling first message"));
            Assert.AreEqual(true, events.Contains("SecondHandler handling second message"));
            Console.WriteLine(string.Join(Environment.NewLine, events));
        }

        class FirstMessage
        {
            public FirstMessage(string message)
            {
                Message = message;
            }
            public string Message;
        }

        class FirstHandler : IHandleMessages<FirstMessage>
        {
            public EventAggregator EventAggregator { get; set; }

            public Task Handle(FirstMessage message)
            {
                EventAggregator.Register($"FirstHandler handling {message.Message}");
                return Task.CompletedTask;
            }
        }

        class SecondMessage
        {
            public SecondMessage(string message)
            {
                Message = message;
            }
            public string Message;
        }

        class SecondHandler : IHandleMessages<SecondMessage>
        {
            public EventAggregator EventAggregator { get; set; }

            public Task Handle(SecondMessage message)
            {
                EventAggregator.Register($"SecondHandler handling {message.Message}");
                return Task.CompletedTask;
            }
        }
    }
}