using Autofac;
using Autofac.Features.Variance;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Exceptions;
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
    class AutofacHandlerActivator : IHandlerActivator
    {
        const string LongExceptionMessage =
            "This particular container builder seems to have had the RegisterRebus(...) extension called on it more than once, which is unfortunately not allowed. In some cases, this is simply an indication that the configuration code for some reason has been executed more than once, which is probably not intended. If you intended to use one Autofac container to host multiple Rebus instances, please consider use the one way bus starter and put your handlers into the multiple handler activator class instead.";

        ILifetimeScope _container;
        private readonly bool _startBus;

        public AutofacHandlerActivator(ContainerBuilder containerBuilder, Action<RebusConfigurer, IComponentContext> configureBus, bool startBus, bool enablePolymorphicDispatch)
        {
            if (containerBuilder == null) throw new ArgumentNullException(nameof(containerBuilder));
            if (configureBus == null) throw new ArgumentNullException(nameof(configureBus));

            if (enablePolymorphicDispatch)
            {
                containerBuilder.RegisterSource(new ContravariantRegistrationSource());
            }

            // Register autofac starter
            containerBuilder.RegisterInstance(this).As<AutofacHandlerActivator>()
                .AutoActivate()
                .SingleInstance()
                .OnActivated(e =>
                {
                    // Make sure we have not been registered multiple times
                    e.Instance._container = e.Context.Resolve<ILifetimeScope>();
                    if (AutofacHelpers.HasMultipleBusRegistrations(e.Instance._container.ComponentRegistry.Registrations))
                    {
                        throw new InvalidOperationException(LongExceptionMessage);
                    }

                    // Start the bus up if requested
                    if (e.Instance._startBus)
                    {
                        try
                        {
                            e.Context.Resolve<IBus>();
                        }
                        catch (Exception exception)
                        {
                            throw new RebusConfigurationException(exception, "Could not start Rebus");
                        }
                    }
                });
            _startBus = startBus;

            // Register IBus. When this is resolved, the bus starts up. This is also disposable, so when the IBus is disposed,
            // it will shut down all it's handlers.
            containerBuilder
                .Register(context =>
                {
                    var rebusConfigurer = Configure.With(this);
                    configureBus.Invoke(rebusConfigurer, context);
                    return rebusConfigurer.Start();
                })
                .SingleInstance();

            // Register ISyncBus
            containerBuilder
                .Register(c => c.Resolve<IBus>().Advanced.SyncBus)
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
            return AutofacHelpers.ResolveAutofacHandlers<TMessage>(transactionContext, _container, _resolvers, null);
        }

        readonly ConcurrentDictionary<Type, Func<ILifetimeScope, IEnumerable<IHandleMessages>>> _resolvers = new ConcurrentDictionary<Type, Func<ILifetimeScope, IEnumerable<IHandleMessages>>>();
    }
}
