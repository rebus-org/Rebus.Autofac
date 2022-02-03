using System;
using Autofac;
using NUnit.Framework;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Utilities;
using Rebus.Transport.InMem;
// ReSharper disable RedundantArgumentDefaultValue
// ReSharper disable ArgumentsStyleNamedExpression

namespace Rebus.Autofac.Tests;

[TestFixture]
public class CheckNewApi : FixtureBase
{
    [Test]
    public void ThisIsHowItWorks()
    {
        var builder = new ContainerBuilder();

        builder.RegisterRebus(
            configure => configure
                .Logging(l => l.Console(minLevel: LogLevel.Debug))
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "ioc-test"))
        );

        var container = builder.Build();

        Using(container);
    }

    [Test]
    public void ThisIsHowItWorks_ComponentContext()
    {
        var builder = new ContainerBuilder();

        builder.Register(_ => new ListLoggerFactory(outputToConsole: true));

        builder.RegisterRebus(
            (configure, context) => configure
                .Logging(l => l.Use(context.Resolve<ListLoggerFactory>()))
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "ioc-test"))
        );

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

        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }

    [Test]
    public void DoesNotThrowWhenRegisteringTwiceIfExplicitlyDisabled()
    {
        var builder = new ContainerBuilder();

        builder.RegisterRebus(configure => configure
                .Logging(l => l.Console(minLevel: LogLevel.Debug))
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "ioc-test")),
            disableMultipleRegistrationsCheck: true);

        builder.RegisterRebus(configure => configure
                .Logging(l => l.Console(minLevel: LogLevel.Debug))
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "ioc-test")),
            disableMultipleRegistrationsCheck: true);

        Assert.DoesNotThrow(() => builder.Build());
    }
}