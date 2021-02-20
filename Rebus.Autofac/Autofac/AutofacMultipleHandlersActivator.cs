using Autofac;
using Autofac.Features.Variance;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Pipeline;
using Rebus.Transport;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
#pragma warning disable 1998

namespace Rebus.Autofac
{
    class AutofacMultipleHandlersActivator<THandlerBase> : IHandlerActivator
        where THandlerBase : class
    {
        ILifetimeScope _container;

        public AutofacMultipleHandlersActivator(ContainerBuilder containerBuilder, Action<RebusConfigurer, IComponentContext> configureBus, bool enablePolymorphicDispatch)
        {
            if (containerBuilder == null) throw new ArgumentNullException(nameof(containerBuilder));
            if (configureBus == null) throw new ArgumentNullException(nameof(configureBus));

            if (enablePolymorphicDispatch)
            {
                containerBuilder.RegisterSource(new ContravariantRegistrationSource());
            }

            // Register autofac starter
            containerBuilder.RegisterInstance(this).As<AutofacMultipleHandlersActivator<THandlerBase>>()
                .AutoActivate()
                .SingleInstance()
                .OnActivated(e =>
                {
                    // Save the container for creating new lifetime scopes
                    e.Instance._container = e.Context.Resolve<ILifetimeScope>();
                });

            // Register IBusStarter so the message handlers can be started up. This is also disposable, so when the IBus is disposed,
            // it will shut down all it's handlers.
            containerBuilder
                .Register(context =>
                {
                    var rebusConfigurer = Configure.With(this);
                    configureBus.Invoke(rebusConfigurer, context);
                    return new BusStarter<THandlerBase>(rebusConfigurer.Create()) as IBusStarter<THandlerBase>;
                })
                .SingleInstance();

            // Register IMessageContext
            containerBuilder
                .Register(c =>
                {
                    var messageContext = MessageContext.Current;
                    if (messageContext == null)
                    {
                        throw new InvalidOperationException("MessageContext.Current was null, which probably means that IMessageContext was resolved outside of a Rebus message handler transaction");
                    }
                    return messageContext;
                })
                .InstancePerDependency()
                .ExternallyOwned();
        }

        /// <summary>
        /// Resolves all handlers for the given <typeparamref name="TMessage"/> message type
        /// </summary>
        public async Task<IEnumerable<IHandleMessages<TMessage>>> GetHandlers<TMessage>(TMessage message, ITransactionContext transactionContext)
        {
            return AutofacHelpers.ResolveAutofacHandlers<TMessage>(transactionContext, _container, _resolvers, typeof(THandlerBase));
        }

        readonly ConcurrentDictionary<Type, Func<ILifetimeScope, IEnumerable<IHandleMessages>>> _resolvers = new ConcurrentDictionary<Type, Func<ILifetimeScope, IEnumerable<IHandleMessages>>>();
    }
}
