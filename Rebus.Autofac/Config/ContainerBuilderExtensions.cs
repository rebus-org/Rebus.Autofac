using System;
using System.Linq;
using System.Reflection;
using Autofac;
using Rebus.Autofac;
using Rebus.Handlers;
using Rebus.Internals;
// ReSharper disable ArgumentsStyleNamedExpression
// ReSharper disable ArgumentsStyleLiteral
// ReSharper disable ObjectCreationAsStatement

namespace Rebus.Config
{
    /// <summary>
    /// Configuration extensions for helping with hooking Rebus up correctly for resolving handlers in Autofac
    /// </summary>
    public static class ContainerBuilderExtensions
    {
        /// <summary>
        /// Makes the necessary registrations in the given <paramref name="containerBuilder"/>, invoking the
        /// <paramref name="configure"/> callback when Rebus needs to be configured.
        /// </summary>
        public static void RegisterRebus(this ContainerBuilder containerBuilder, Func<RebusConfigurer, RebusConfigurer> configure)
        {
            if (containerBuilder == null) throw new ArgumentNullException(nameof(containerBuilder));
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            new AutofacHandlerActivator(containerBuilder, (configurer, context) => configure(configurer), startBus: true, enablePolymorphicDispatch: false);
        }

        /// <summary>
        /// Makes the necessary registrations in the given <paramref name="containerBuilder"/>, invoking the
        /// <paramref name="configure"/> callback when Rebus needs to be configured.
        /// </summary>
        public static void RegisterRebus(this ContainerBuilder containerBuilder, Func<RebusConfigurer, IComponentContext, RebusConfigurer> configure)
        {
            if (containerBuilder == null) throw new ArgumentNullException(nameof(containerBuilder));
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            new AutofacHandlerActivator(containerBuilder, (configurer, context) => configure(configurer, context), startBus: true, enablePolymorphicDispatch: false);
        }

        /// <summary>
        /// Registers all Rebus message handler types found in the assembly of <typeparamref name="T"/>
        /// </summary>
        public static void RegisterHandlersFromAssemblyOf<T>(this ContainerBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            builder.RegisterAssemblyTypes(typeof(T).Assembly)
                .Where(t => !t.IsAbstract && t.GetInterfaces().Any(IsRebusHandler))
                .AsImplementedInterfaces()
                .InstancePerDependency()
                .PropertiesAutowired();
        }

        /// <summary>
        /// Registers the given type as a Rebus message handler
        /// </summary>
        public static void RegisterHandler<T>(this ContainerBuilder builder) where T : IHandleMessages
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            var implementedHandlerTypes = typeof(T).GetInterfaces().Where(IsRebusHandler).ToArray();

            builder.RegisterType(typeof(T)).As(implementedHandlerTypes)
                .InstancePerDependency()
                .PropertiesAutowired();
        }

        static bool IsRebusHandler(Type i) => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IHandleMessages<>);
    }
}