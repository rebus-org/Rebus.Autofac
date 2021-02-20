using Autofac;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Transport;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Rebus.Autofac
{
    class AutofacOneWayBusActivator : IHandlerActivator
    {
        const string MoreThanOneBusExceptionMessage =
            "This particular container builder seems to have had the RegisterOneWayRebus(...) extension called on it more than once, which is unfortunately not allowed. This is simply an indication that the configuration code for some reason has been executed more than once, which is probably not intended.";
        const string NotOneWayBusExceptionMessage =
            "This particular container builder has NOT been configured as a one way bus. Please make sure the transport is configured as a one way bus so there are no handler threads required.";

        public AutofacOneWayBusActivator(ContainerBuilder containerBuilder, Action<RebusConfigurer, IComponentContext> configureBus)
        {
            if (containerBuilder == null) throw new ArgumentNullException(nameof(containerBuilder));
            if (configureBus == null) throw new ArgumentNullException(nameof(configureBus));

            // Register autofac one way bus starter and make sure it was not registered multiple times
            containerBuilder.RegisterInstance(this).As<AutofacOneWayBusActivator>()
                .AutoActivate()
                .SingleInstance()
                .OnActivated(e =>
                {
                    var container = e.Context.Resolve<ILifetimeScope>();
                    if (AutofacHelpers.HasMultipleBusRegistrations(container.ComponentRegistry.Registrations))
                    {
                        throw new InvalidOperationException(MoreThanOneBusExceptionMessage);
                    }
                });

            // Register IBus. This is a one way bus so it does not actually start anything. We also make sure it's configured as a one way bus.
            containerBuilder
                .Register(context =>
                {
                    var rebusConfigurer = Configure.With(this);
                    configureBus.Invoke(rebusConfigurer, context);
                    var bus = rebusConfigurer.Start();
                    if (bus.Advanced.Workers.Count > 0)
                    {
                        throw new InvalidOperationException(NotOneWayBusExceptionMessage);
                    }
                    return bus;
                })
                .SingleInstance();

            // register ISyncBus
            containerBuilder
                .Register(c => c.Resolve<IBus>().Advanced.SyncBus)
                .SingleInstance();
        }

        /// <summary>
        /// Resolves all handlers for the given <typeparamref name="TMessage"/> message type
        /// </summary>
        public Task<IEnumerable<IHandleMessages<TMessage>>> GetHandlers<TMessage>(TMessage message, ITransactionContext transactionContext)
        {
            throw new InvalidOperationException(NotOneWayBusExceptionMessage);
        }
    }
}