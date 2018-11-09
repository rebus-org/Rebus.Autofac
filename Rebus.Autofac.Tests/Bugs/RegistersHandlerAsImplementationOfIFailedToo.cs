using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using NUnit.Framework;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Retry.Simple;
using Rebus.Transport;

namespace Rebus.Autofac.Tests.Bugs
{
    [TestFixture]
    public class RegistersHandlerAsImplementationOfIFailedToo
    {
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