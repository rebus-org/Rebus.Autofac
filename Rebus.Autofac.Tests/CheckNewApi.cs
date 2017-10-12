using System;
using Autofac;
using NUnit.Framework;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Tests.Contracts;
using Rebus.Transport.InMem;
// ReSharper disable RedundantArgumentDefaultValue
// ReSharper disable ArgumentsStyleNamedExpression

namespace Rebus.Autofac.Tests
{
    [TestFixture]
    public class CheckNewApi : FixtureBase
    {
        [Test]
        public void ThisIsHowItWorks()
        {
            var builder = new ContainerBuilder();

            builder.RegisterRebus(configure => configure
                .Logging(l => l.Console(minLevel: LogLevel.Debug))
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "ioc-test")));

            var container = builder.Build();

            Using(container);
        }

        [Test]
        public void ThrowsWhenAddingTwice()
        {
            var builder = new ContainerBuilder();

            builder.RegisterRebus(configure => configure
                .Logging(l => l.Console(minLevel: LogLevel.Debug))
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "ioc-test")));

            builder.RegisterRebus(configure => configure
                .Logging(l => l.Console(minLevel: LogLevel.Debug))
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "ioc-test")));

            Assert.Throws<InvalidOperationException>(() =>
            {
                builder.Build();
            });
        }
    }
}