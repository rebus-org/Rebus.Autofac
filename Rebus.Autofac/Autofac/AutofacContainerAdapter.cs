using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Autofac.Core;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Extensions;
using Rebus.Handlers;
using Rebus.Pipeline;
using Rebus.Transport;

#pragma warning disable 618
#pragma warning disable 1998

namespace Rebus.Autofac
{
    /// <summary>
    /// Implementation of <see cref="IContainerAdapter"/> that is backed by an Autofac container
    /// </summary>
    public class AutofacContainerAdapter : IContainerAdapter
    {
        readonly IContainer _container;

        /// <summary>
        /// Constructs the adapter, using the specified container
        /// </summary>
        public AutofacContainerAdapter(IContainer container)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
        }

        /// <summary>
        /// Resolves all handlers for the given <typeparamref name="TMessage"/> message type
        /// </summary>
        public async Task<IEnumerable<IHandleMessages<TMessage>>> GetHandlers<TMessage>(TMessage message, ITransactionContext transactionContext)
        {
            var lifetimeScope = transactionContext
                .GetOrAdd("current-autofac-lifetime-scope", () =>
                {
                    var scope = _container.BeginLifetimeScope();

                    transactionContext.OnDisposed(() => scope.Dispose());

                    return scope;
                });

            var handledMessageTypes = typeof(TMessage).GetBaseTypes()
                .Concat(new[] { typeof(TMessage) });

            return handledMessageTypes
                .SelectMany(handledMessageType =>
                {
                    var implementedInterface = typeof(IHandleMessages<>).MakeGenericType(handledMessageType);
                    var implementedInterfaceSequence = typeof(IEnumerable<>).MakeGenericType(implementedInterface);

                    return (IEnumerable<IHandleMessages>)lifetimeScope.Resolve(implementedInterfaceSequence);
                })
                .Cast<IHandleMessages<TMessage>>();
        }

        /// <summary>
        /// Stores the bus instance
        /// </summary>
        public void SetBus(IBus bus)
        {
            if (_container.ComponentRegistry.IsRegistered(new TypedService(typeof(IBus))))
            {
                throw new InvalidOperationException("Cannot add another IBus registration to this container. If you want multiple bus instances in a single process, please host them in one container per bus instance.");
            }

            var containerBuilder = new ContainerBuilder();

            containerBuilder
                .RegisterInstance(bus)
                    .SingleInstance();

            containerBuilder
                .Register(c => c.Resolve<IBus>().Advanced.SyncBus)
                    .InstancePerDependency()
                    .ExternallyOwned();

            containerBuilder
                .Register(c =>
                    {
                        var currentMessageContext = MessageContext.Current;
                        if (currentMessageContext == null)
                        {
                            throw new InvalidOperationException("Attempted to inject the current message context from MessageContext.Current, but it was null! Did you attempt to resolve IMessageContext from outside of a Rebus message handler?");
                        }
                        return currentMessageContext;
                    })
                    .InstancePerDependency()
                    .ExternallyOwned();

            containerBuilder.Update(_container);
        }
    }
}
