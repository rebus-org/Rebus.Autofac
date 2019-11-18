using Autofac;
using Autofac.Core;
using Autofac.Features.Variance;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Exceptions;
using Rebus.Extensions;
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
#pragma warning disable 1998

namespace Rebus.Autofac
{
    class AutofacHandlerActivator : IHandlerActivator
    {
        const string LongExceptionMessage =
            "This particular container builder seems to have had the RegisterRebus(...) extension called on it more than once, which is unfortunately not allowed. In some cases, this is simply an indication that the configuration code for some reason has been executed more than once, which is probably not intended. If you intended to use one Autofac container to host multiple Rebus instances, please consider using a separate container instance for each Rebus endpoint that you wish to start.";

        ILifetimeScope _container;
        private readonly bool _startBus;
        private readonly object _customLifetimeTags;

        public AutofacHandlerActivator(ContainerBuilder containerBuilder, Action<RebusConfigurer, IComponentContext> configureBus, bool startBus, bool enablePolymorphicDispatch, object customLifetimeTags = null)
        {
            if (containerBuilder == null) throw new ArgumentNullException(nameof(containerBuilder));
            if (configureBus == null) throw new ArgumentNullException(nameof(configureBus));

            if (enablePolymorphicDispatch)
            {
                containerBuilder.RegisterSource(new ContravariantRegistrationSource());
            }

            //register autofac starter
            containerBuilder.RegisterInstance(this).As<AutofacHandlerActivator>()
                .AutoActivate()
                .SingleInstance()
                .OnActivated((e)=> {
                    if (e.Instance._container == null)
                    {
                        e.Instance._container = e.Context.Resolve<ILifetimeScope>();
                    }
                    if (HasMultipleBusRegistrations(e.Instance._container.ComponentRegistry.Registrations))
                    {
                        throw new InvalidOperationException(LongExceptionMessage);
                    }

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
            _customLifetimeTags = customLifetimeTags;

            // register IBus
            containerBuilder
                .Register(context =>
                {
                    if (_container == null)
                    {
                        _container = context.Resolve<ILifetimeScope>();
                    }

                    var rebusConfigurer = Configure.With(this);
                    configureBus.Invoke(rebusConfigurer, context);
                    return rebusConfigurer.Start();
                })
                .SingleInstance();

            // regiser ISyncBus
            containerBuilder
                .Register(c => c.Resolve<IBus>().Advanced.SyncBus)
                .InstancePerDependency()
                .ExternallyOwned();

            // register IMessageContext
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

        static bool HasMultipleBusRegistrations(IEnumerable<IComponentRegistration> registrations) =>
            registrations.SelectMany(r => r.Services)
                .OfType<TypedService>()
                .Count(s => s.ServiceType == typeof(IBus)) > 1;

        /// <summary>
        /// Resolves all handlers for the given <typeparamref name="TMessage"/> message type
        /// </summary>
        public async Task<IEnumerable<IHandleMessages<TMessage>>> GetHandlers<TMessage>(TMessage message, ITransactionContext transactionContext)
        {
            ILifetimeScope CreateLifetimeScope()
            {
                var scope = (_customLifetimeTags != null) ?
                    _container.BeginLifetimeScope(_customLifetimeTags) :
                    _container.BeginLifetimeScope();
                transactionContext.OnDisposed(() => scope.Dispose());
                return scope;
            }

            var lifetimeScope = transactionContext
                .GetOrAdd("current-autofac-lifetime-scope", CreateLifetimeScope);

            var resolver = _resolvers.GetOrAdd(typeof(TMessage),
                messageType =>
                {
                    if (messageType.IsAssignableTo(typeof(IFailed<>)))
                    {
                        var containedMessageType = messageType.GetGenericTypeParameters(typeof(IFailed<>)).Single();
                        var additionalTypesToResolveHandlersFor = containedMessageType.GetBaseTypes();
                        var typesToResolve = new[] { containedMessageType }
                            .Concat(additionalTypesToResolveHandlersFor)
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
                });

            return (IEnumerable<IHandleMessages<TMessage>>)resolver(lifetimeScope);
        }

        readonly ConcurrentDictionary<Type, Func<ILifetimeScope, IEnumerable<IHandleMessages>>> _resolvers = new ConcurrentDictionary<Type, Func<ILifetimeScope, IEnumerable<IHandleMessages>>>();
    }
}
