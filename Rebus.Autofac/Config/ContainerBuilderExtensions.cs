using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using Autofac.Core;
using Rebus.Autofac;
using Rebus.Bus;
using Rebus.Pipeline;

namespace Rebus.Config
{
    /// <summary>
    /// Configuration extensions for helping with hooking Rebus up correctly for resolving handlers in Autofac
    /// </summary>
    public static class ContainerBuilderExtensions
    {
        const string LongExceptionMessage = 
            "This particular container builder seems to have had the AddRebus(...) extension called on it more than once, which is unfortunately not allowed. In some cases, this is simply an indication that the configuration code for some reason has been executed more than once, which is probably not intended. If you intended to use one Autofac container to host multiple Rebus instances, please consider using a separate container instance for each Rebus endpoint that you wish to start.";

        /// <summary>
        /// Makes the necessary registrations in the given <paramref name="containerBuilder"/>, invoking the
        /// <paramref name="configure"/> callback when Rebus needs to be configured.
        /// </summary>
        public static void AddRebus(this ContainerBuilder containerBuilder, Func<RebusConfigurer, RebusConfigurer> configure)
        {
            if (containerBuilder == null) throw new ArgumentNullException(nameof(containerBuilder));
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            var activator = new AutofacContainerAdapter2();

            containerBuilder.RegisterBuildCallback(c =>
            {
                var registrations = c.ComponentRegistry.Registrations;

                if (!HasMultipleBusRegistrations(registrations))
                {
                    activator.SetContainer(c);
                    return;
                }

                throw new InvalidOperationException(LongExceptionMessage);
            });

            containerBuilder
                .Register(context =>
                {
                    var rebusConfigurer = Configure.With(activator);
                    configure(rebusConfigurer);
                    return rebusConfigurer.Start();
                })
                .SingleInstance();

            containerBuilder
                .Register(c => c.Resolve<IBus>().Advanced.SyncBus)
                .InstancePerRequest();

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
                .InstancePerRequest();
        }

        static bool HasMultipleBusRegistrations(IEnumerable<IComponentRegistration> registrations) =>
            registrations.SelectMany(r => r.Services)
                .OfType<TypedService>()
                .Count(s => s.ServiceType == typeof(IBus)) > 1;
    }
}