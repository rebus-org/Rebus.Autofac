using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using AdditionalHandlerAssembly;
using Autofac;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Retry.Simple;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;

namespace Rebus.Autofac.Tests.Bugs;

[TestFixture]
public class ReproduceDoubleDispatchIssue : FixtureBase
{
    [Test]
    [Description("Tried to reproduce issue reported: Messages should apparently be dispatched twice. Sp far, reproduction has not been not successful.")]
    public async Task ItWorks()
    {
        var controllerChangeMessages = new ConcurrentQueue<ControllerChangeMessage>();

        var builder = new ContainerBuilder();

        builder.RegisterRebus((configurer, _) =>
        {
            var queueName = "queue";

            configurer
                .Logging(l => l.ColoredConsole())
                .Transport(x => x.UseInMemoryTransport(new InMemNetwork(true), queueName))
                .Options(x =>
                {
                    x.SetMaxParallelism(1);
                    x.SimpleRetryStrategy(maxDeliveryAttempts: int.MaxValue);
                });

            return configurer;
        });

        builder.RegisterHandlersFromAssemblyOf<ControllerChangeMessageHandler>();

        builder.RegisterInstance(controllerChangeMessages).AsSelf();

        await using var container = builder.Build();

        var bus = container.Resolve<IBus>();

        await bus.SendLocal(new ControllerChangeMessage());

        // wait for at least one message to arrive
        await controllerChangeMessages.WaitUntil(q => q.Count >= 1);

        // wait additional time to be sure all messages have been processed
        await Task.Delay(TimeSpan.FromSeconds(2));

        // even at this point, the handler should only have been called once!
        Assert.That(controllerChangeMessages.Count, Is.EqualTo(1));
    }
}