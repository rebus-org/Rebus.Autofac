using Autofac;
using Autofac.Core;
using Autofac.Features.Variance;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Exceptions;
using Rebus.Handlers;
using Rebus.Internals;
using Rebus.Pipeline;
using Rebus.Retry.Simple;
using Rebus.Transport;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
// ReSharper disable SimplifyLinqExpressionUseAll
#pragma warning disable 1998

namespace Rebus.Autofac;

class AutofacHandlerActivator : IHandlerActivator
{
    const string LongExceptionMessage =
        "This particular container builder seems to have had the RegisterRebus(...) extension called on it more than once, which is unfortunately not allowed. In some cases, this is simply an indication that the configuration code for some reason has been executed more than once, which is probably not intended. If you intended to use one Autofac container to host multiple Rebus instances, please consider using a separate container instance for each Rebus endpoint that you wish to start.";

    readonly ConcurrentDictionary<Type, Func<ILifetimeScope, IEnumerable<IHandleMessages>>> _resolvers = new();

    ILifetimeScope _container;

    public AutofacHandlerActivator(ContainerBuilder containerBuilder, Action<RebusConfigurer, IComponentContext> configureBus, bool startBus, bool enablePolymorphicDispatch, bool multipleRegistrationsCheckEnabled)
    {
        if (containerBuilder == null) throw new ArgumentNullException(nameof(containerBuilder));
        if (configureBus == null) throw new ArgumentNullException(nameof(configureBus));

        if (enablePolymorphicDispatch)
        {
            containerBuilder.RegisterSource(new ContravariantRegistrationSource());
        }

        if (multipleRegistrationsCheckEnabled)
        {
            var autofacHandlerActivatorWasRegistered = false;

            // guard against additional calls to RegisterRebus by detecting number of calls here
            containerBuilder.ComponentRegistryBuilder.Registered += (_, ea) =>
            {
                var registration = ea.ComponentRegistration;
                var typedServices = registration.Services.OfType<TypedService>();

                if (!typedServices.Any(t => t.ServiceType == typeof(AutofacHandlerActivator))) return;

                if (autofacHandlerActivatorWasRegistered)
                {
                    throw new InvalidOperationException(LongExceptionMessage);
                }

                autofacHandlerActivatorWasRegistered = true;
            };
        }

        // Register autofac starter
        containerBuilder.RegisterInstance(this).As<AutofacHandlerActivator>()
            .AutoActivate()
            .SingleInstance()
            .OnActivated(e =>
            {
                e.Instance._container ??= e.Context.Resolve<ILifetimeScope>();

                if (!startBus) return;

                // Start the bus up if requested
                try
                {
                    var busStarter = e.Context.Resolve<IBusStarter>();

                    busStarter.Start();
                }
                catch (Exception exception)
                {
                    throw new RebusConfigurationException(exception, "Could not start Rebus");
                }
            });

        // Register IBusStarter
        containerBuilder
            .Register(context =>
            {
                _container ??= context.Resolve<ILifetimeScope>();

                var rebusConfigurer = Configure.With(this);
                configureBus.Invoke(rebusConfigurer, context);
                return rebusConfigurer.Create();
            })
            .SingleInstance();

        // Register IBus
        containerBuilder
            .Register(context => context.Resolve<IBusStarter>().Bus)
            .SingleInstance();

        // Register ISyncBus, resolved from IBus
        containerBuilder
            .Register(c => c.Resolve<IBus>().Advanced.SyncBus)
            .InstancePerDependency()
            .ExternallyOwned();

        // Register IMessageContext
        containerBuilder
            .Register(_ =>
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
        ILifetimeScope CreateLifetimeScope()
        {
            var scope = _container.BeginLifetimeScope();
            transactionContext.OnDisposed(_ => scope.Dispose());
            return scope;
        }

        Func<ILifetimeScope, IEnumerable<IHandleMessages>> GetResolveForMessageType(Type messageType)
        {
            if (messageType.IsAssignableTo(typeof(IFailed<>)))
            {
                var containedMessageType = messageType.GetGenericTypeParameters(typeof(IFailed<>)).Single();
                var additionalTypesToResolveHandlersFor = containedMessageType.GetBaseTypes(includeSelf: false);
                var typesToResolve = new[] { containedMessageType }.Concat(additionalTypesToResolveHandlersFor)
                    .Select(type => typeof(IEnumerable<>).MakeGenericType(typeof(IHandleMessages<>).MakeGenericType(typeof(IFailed<>).MakeGenericType(type))))
                    .ToArray();

                return scope =>
                {
                    var handlers = new List<IHandleMessages<TMessage>>();

                    foreach (var type in typesToResolve)
                    {
                        handlers.AddRange((IEnumerable<IHandleMessages<TMessage>>)scope.Resolve(type));
                    }

                    return handlers;
                };
            }

            return scope => scope.Resolve<IEnumerable<IHandleMessages<TMessage>>>();
        }

        var lifetimeScope = transactionContext.GetOrAdd("current-autofac-lifetime-scope", CreateLifetimeScope);
        var resolver = _resolvers.GetOrAdd(typeof(TMessage), GetResolveForMessageType);

        return (IEnumerable<IHandleMessages<TMessage>>)resolver(lifetimeScope);
    }
}