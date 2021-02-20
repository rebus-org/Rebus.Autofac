﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Autofac.Features.Variance;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Retry.Simple;
using Rebus.Tests.Contracts;
using Rebus.Transport;
using Rebus.Transport.InMem;
#pragma warning disable 1998

namespace Rebus.Autofac.Tests.Bugs
{
    [TestFixture]
    public class RegistersHandlerAsImplementationOfIFailedToo : FixtureBase
    {
        [Test, Description("Verifies that a bus using Autofac and Rebus' handler registration API CAN in fact received failed messages")]
        public async Task DoItWithTheBus()
        {
            var builder = new ContainerBuilder();

            builder.RegisterSource(new ContravariantRegistrationSource());

            builder.RegisterRebus(configurer => configurer
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "2nd-level-test"))
                .Options(o => o.SimpleRetryStrategy(
                    secondLevelRetriesEnabled: true,
                    maxDeliveryAttempts: 1
                ))
            );

            builder.RegisterHandler<SecondLevelHandler>();

            var failedMessageWasReceived = new ManualResetEvent(false);

            builder.RegisterInstance(failedMessageWasReceived);

            using (var container = builder.Build())
            {
                await container.Resolve<IBus>().SendLocal("HEJ MED DIG MIN VEN");

                if (!failedMessageWasReceived.WaitOne(TimeSpan.FromSeconds(5)))
                {
                    throw new AssertionException("Failed message was NOT received within 5s timeout!");
                }
            }
        }

        class SecondLevelHandler : IHandleMessages<string>, IHandleMessages<IFailed<string>>
        {
            readonly ManualResetEvent _gotTheFailedMessage;

            public SecondLevelHandler(ManualResetEvent gotTheFailedMessage) => _gotTheFailedMessage = gotTheFailedMessage;

            public Task Handle(string message) => throw new NotImplementedException("MUAHAHAHAHAHA");

            public async Task Handle(IFailed<string> message) => _gotTheFailedMessage.Set();
        }

        [Test]
        public async Task ReadTheFixtureName()
        {
            var builder = new ContainerBuilder();

            builder.RegisterHandler<TestHandler>();

            using (var container = builder.Build())
            {
                var ordinaryHandlers = container.Resolve<IEnumerable<IHandleMessages<string>>>();
                var failedMessageHandlers = container.Resolve<IEnumerable<IHandleMessages<IFailed<string>>>>();

                Assert.That(ordinaryHandlers.Count(), Is.EqualTo(1));
                Assert.That(failedMessageHandlers.Count(), Is.EqualTo(1));
            }
        }

        [Test]
        public async Task ResolvesFailedOfBaseClass()
        {
            var builder = new ContainerBuilder();
            builder.RegisterHandler<TestHandler>();

            var activator = new AutofacHandlerActivator(builder, (_, __) => { }, startBus: false, enablePolymorphicDispatch: true);
            using (var container = builder.Build())
            using (var scope = new RebusTransactionScope())
            {
                var handlers = await activator.GetHandlers<string>(null, scope.TransactionContext);
                Assert.That(handlers, Is.Not.Empty, "resolving handlers for derived messages failed");

                var handlersFailed = await activator.GetHandlers<IFailed<string>>(null, scope.TransactionContext);
                Assert.That(handlersFailed, Is.Not.Empty, "resolving handlers for failed derived messages failed");
            }
        }

        public class FailedMessage<T> : IFailed<T>
        {
            public FailedMessage(T message, string errorDescription, Dictionary<string, string> headers, IEnumerable<Exception> exceptions)
            {
                Message = message;
                ErrorDescription = errorDescription;
                Headers = headers;
                Exceptions = exceptions;
            }

            public T Message { get; }
            public string ErrorDescription { get; }
            public Dictionary<string, string> Headers { get; }
            public IEnumerable<Exception> Exceptions { get; }
        }

        class TestHandler : IHandleMessages<string>, IHandleMessages<IFailed<string>>
        {
            public Task Handle(string message)
            {
                throw new System.NotImplementedException();
            }

            public Task Handle(IFailed<string> message)
            {
                throw new System.NotImplementedException();
            }
        }
    }
}