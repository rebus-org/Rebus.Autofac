using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Autofac.Core;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Exceptions;
using Rebus.Extensions;
using Rebus.Handlers;
using Rebus.Pipeline;
using Rebus.Transport;

namespace Rebus.Autofac
{
    class AutofacContainerAdapter2 : IHandlerActivator
    {
        const string LongExceptionMessage =
            "This particular container builder seems to have had the RegisterRebus(...) extension called on it more than once, which is unfortunately not allowed. In some cases, this is simply an indication that the configuration code for some reason has been executed more than once, which is probably not intended. If you intended to use one Autofac container to host multiple Rebus instances, please consider using a separate container instance for each Rebus endpoint that you wish to start.";

        IContainer _container;

        public AutofacContainerAdapter2(ContainerBuilder containerBuilder, Action<RebusConfigurer> configureBus, bool startBus = true)
        {
            if (containerBuilder == null) throw new ArgumentNullException(nameof(containerBuilder));
            if (configureBus == null) throw new ArgumentNullException(nameof(configureBus));

            containerBuilder.RegisterBuildCallback(container =>
            {
                var registrations = container.ComponentRegistry.Registrations;

                if (HasMultipleBusRegistrations(registrations))
                {
                    throw new InvalidOperationException(LongExceptionMessage);
                }

                SetContainer(container);

                if (startBus)
                {
                    StartBus(container);
                }
            });

            containerBuilder
                .Register(context =>
                {
                    var rebusConfigurer = Configure.With(this);
                    configureBus.Invoke(rebusConfigurer);
                    return rebusConfigurer.Start();
                })
                .SingleInstance();

            containerBuilder
                .Register(c => c.Resolve<IBus>().Advanced.SyncBus)
                .InstancePerDependency()
                .ExternallyOwned();

            containerBuilder
                .Register(c =>
                {
                    var messageContext = MessageContext.Current;
                    if (messageContext == null)
                    {
                        throw new InvalidOperationException("MessageContext.Current was null, which probably means that IMessageContext was resolve outside of a Rebus message handler transaction");
                    }
                    return messageContext;
                })
                .InstancePerDependency()
                .ExternallyOwned();
        }

        static void StartBus(IContainer c)
        {
            try
            {
                c.Resolve<IBus>();
            }
            catch (Exception exception)
            {
                throw new RebusConfigurationException(exception, "Could not start Rebus");
            }
        }

        static bool HasMultipleBusRegistrations(IEnumerable<IComponentRegistration> registrations) =>
            registrations.SelectMany(r => r.Services)
                .OfType<TypedService>()
                .Count(s => s.ServiceType == typeof(IBus)) > 1;

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

        void SetContainer(IContainer container)
        {
            if (_container != null)
            {
                throw new InvalidOperationException("One container instance can only have its SetContainer method called once");
            }
            _container = container ?? throw new ArgumentNullException(nameof(container), "Please pass a container instance when calling this method");
        }
    }
}