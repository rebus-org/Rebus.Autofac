using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Autofac.Core;
using MessageHandlers;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Bus.Advanced;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Logging;
using Rebus.Transport.InMem;
// ReSharper disable RedundantArgumentDefaultValue
// ReSharper disable ArgumentsStyleNamedExpression

namespace Rebus.Autofac.Tests
{
    [TestFixture]
    public class TestRegistrationApi
    {
        [Test]
        public void ItWorks_Single()
        {
            var builder = new ContainerBuilder();

            builder.RegisterHandler<MsgHndlr>();

            using (var container = builder.Build())
            {
                var stringHandlers = container.Resolve<IEnumerable<IHandleMessages<string>>>().ToList();

                Assert.That(stringHandlers.Count, Is.EqualTo(1));
                Assert.That(stringHandlers[0], Is.TypeOf<MsgHndlr>());
            }
        }

        class MsgHndlr : IHandleMessages<string>
        {
            public Task Handle(string message)
            {
                throw new System.NotImplementedException();
            }
        }

        [Test]
        public void ItWorks_AssemblyOf()
        {
            var builder = new ContainerBuilder();

            builder.RegisterType<EventAggregator>().SingleInstance();
            builder.RegisterHandlersFromAssemblyOf<FirstStringHandler>();

            using (var container = builder.Build())
            {
                var stringHandlers = container.Resolve<IEnumerable<IHandleMessages<string>>>().OrderBy(t => t.GetType().FullName) .ToList();

                Assert.That(stringHandlers.Count, Is.EqualTo(2));
                Assert.That(stringHandlers[0], Is.TypeOf<FirstStringHandler>());
                Assert.That(stringHandlers[1], Is.TypeOf<SecondStringHandler>());
            }
        }

        [Test]
        public void RegisterRebusThrowsWhenAddingTwice()
        {
            var builder = new ContainerBuilder();

            builder.RegisterRebus(configure => configure
                .Logging(l => l.Console(minLevel: LogLevel.Debug))
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "ioc-test")));

            builder.RegisterRebus(configure => configure
                .Logging(l => l.Console(minLevel: LogLevel.Debug))
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "ioc-test")));

            Assert.Throws<DependencyResolutionException>(() =>
            {
                builder.Build();
            });
        }

        [Test]
        public async Task RealBusAndStuff_Single()
        {
            // First register some handlers
            var builder = new ContainerBuilder();
            builder.RegisterHandler<FirstStringHandler>();
            builder.RegisterHandler<SecondStringHandler>();
            builder.RegisterType<EventAggregator>().SingleInstance();

            // Now register the bus
            builder.RegisterRebus(
                configurer => configurer
                    .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "doesn't matter"))
                    .Options(o =>
                    {
                        o.SetNumberOfWorkers(1);
                        o.SetMaxParallelism(1);
                    })
            );

            // Build the IoC container, which starts the bus
            using (var container = builder.Build())
            {
                // Test sending a message via the async bus
                var bus = container.Resolve<IBus>();
                await bus.SendLocal("HEJ MED DIG");

                // Test sending a message via the sync bus
                var syncBus = container.Resolve<ISyncBus>();
                syncBus.SendLocal("HVORDAN GÅR DET?");

                // Wait for the messages to get received in the handler threads
                await Task.Delay(500);

                // Check it all worked
                var events = container.Resolve<EventAggregator>().OrderBy(s => s).ToList();
                Assert.That(events.Count, Is.EqualTo(4));
                Assert.AreEqual(events[0], "FirstHandler handling HEJ MED DIG");
                Assert.AreEqual(events[1], "FirstHandler handling HVORDAN GÅR DET?");
                Assert.AreEqual(events[2], "SecondHandler handling HEJ MED DIG");
                Assert.AreEqual(events[3], "SecondHandler handling HVORDAN GÅR DET?");
                Console.WriteLine(string.Join(Environment.NewLine, events));
            }
        }

        [Test]
        public async Task RealBusAndStuff_AssemblyOf()
        {
            // First register some handlers. There will be three of them.
            var builder = new ContainerBuilder();
            builder.RegisterHandlersFromAssemblyOf<FirstStringHandler>();
            builder.RegisterType<EventAggregator>().SingleInstance();

            // Now register the bus
            builder.RegisterRebus(
                configurer => configurer
                    .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "doesn't matter"))
                    .Options(o =>
                    {
                        o.SetNumberOfWorkers(1);
                        o.SetMaxParallelism(1);
                    })
            );

            // Build the IoC container, which starts the bus
            using (var container = builder.Build())
            {
                // Test sending a message via the async bus
                var bus = container.Resolve<IBus>();
                await bus.SendLocal("HEJ MED DIG");

                // Test sending a message via the sync bus
                var syncBus = container.Resolve<ISyncBus>();
                syncBus.SendLocal("HVORDAN GÅR DET?");

                // Wait for the messages to get received in the handler threads
                await Task.Delay(500);

                // Check it all worked
                var events = container.Resolve<EventAggregator>().OrderBy(s => s).ToList();
                Assert.That(events.Count, Is.EqualTo(4));
                Assert.AreEqual(events[0], "FirstHandler handling HEJ MED DIG");
                Assert.AreEqual(events[1], "FirstHandler handling HVORDAN GÅR DET?");
                Assert.AreEqual(events[2], "SecondHandler handling HEJ MED DIG");
                Assert.AreEqual(events[3], "SecondHandler handling HVORDAN GÅR DET?");
                Console.WriteLine(string.Join(Environment.NewLine, events));
            }
        }

        private IContainer _container;

        [Test]
        public async Task RealBusAndStuff_LifetimeScopeCallback()
        {
            // First register a handler
            var builder = new ContainerBuilder();
            builder.RegisterHandler<FirstStringHandler>();
            builder.RegisterType<EventAggregator>().SingleInstance();

            // Now register the bus
            builder.RegisterRebus(
                configurer => configurer
                    .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "doesn't matter"))
                    .Options(o =>
                    {
                        o.SetNumberOfWorkers(1);
                        o.SetMaxParallelism(1);
                    }));

            // Build the IoC container, which starts the bus
            using (_container = builder.Build())
            {
                // Test sending a message via the async bus
                var bus = _container.Resolve<IBus>();
                await bus.SendLocal("HEJ MED DIG");


                // Wait for the messages to get received in the handler threads
                await Task.Delay(500);

                // Check it all worked
                var events = _container.Resolve<EventAggregator>().ToList();
                Assert.That(events.Count, Is.EqualTo(1));
                Assert.AreEqual(events[0], "FirstHandler handling HEJ MED DIG");
                Console.WriteLine(string.Join(Environment.NewLine, events));
            }
        }
    }
}