using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using MessageHandlers;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Transport.InMem;

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
        public void ItWorks_Multiple()
        {
            var builder = new ContainerBuilder();

            builder.RegisterType<EventAggregator>().SingleInstance();
            builder.RegisterHandlersFromAssemblyOf<FirstHandler>();

            using (var container = builder.Build())
            {
                var stringHandlers = container.Resolve<IEnumerable<IHandleMessages<string>>>().ToList();

                Assert.That(stringHandlers.Count, Is.EqualTo(2));
            }
        }

        [Test]
        public async Task RealBusAndStuff_Single()
        {
            var builder = new ContainerBuilder();

            builder.RegisterHandler<FirstHandler>();
            builder.RegisterHandler<SecondHandler>();

            builder.RegisterType<EventAggregator>().SingleInstance();
            builder.RegisterRebus(
                configurer => configurer
                    .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "doesn't matter"))
                    .Options(o =>
                    {
                        o.SetNumberOfWorkers(1);
                        o.SetMaxParallelism(1);
                    })
            );

            using (var container = builder.Build())
            {
                var bus = container.Resolve<IBus>();

                await bus.SendLocal("HEJ MED DIG");
                await bus.SendLocal("HVORDAN GÅR DET?");

                await Task.Delay(500);

                var events = container.Resolve<EventAggregator>().ToList();

                Assert.That(events.Count, Is.EqualTo(4));
                Console.WriteLine(string.Join(Environment.NewLine, events));
            }
        }

        [Test]
        public async Task RealBusAndStuff_Multiple()
        {
            var builder = new ContainerBuilder();

            builder.RegisterHandlersFromAssemblyOf<FirstHandler>();

            builder.RegisterType<EventAggregator>().SingleInstance();
            builder.RegisterRebus(
                configurer => configurer
                    .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "doesn't matter"))
                    .Options(o =>
                    {
                        o.SetNumberOfWorkers(1);
                        o.SetMaxParallelism(1);
                    })
            );

            using (var container = builder.Build())
            {
                var bus = container.Resolve<IBus>();

                await bus.SendLocal("HEJ MED DIG");
                await bus.SendLocal("HVORDAN GÅR DET?");

                await Task.Delay(500);

                var events = container.Resolve<EventAggregator>().ToList();

                Assert.That(events.Count, Is.EqualTo(4));
                Console.WriteLine(string.Join(Environment.NewLine, events));
            }
        }
    }
}