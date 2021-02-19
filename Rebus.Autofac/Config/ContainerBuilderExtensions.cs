using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using Rebus.Autofac;
using Rebus.Handlers;

// ReSharper disable ArgumentsStyleNamedExpression
// ReSharper disable ArgumentsStyleLiteral
// ReSharper disable ObjectCreationAsStatement
// ReSharper disable UnusedMember.Global

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
        public static void RegisterRebus(this ContainerBuilder containerBuilder, Func<RebusConfigurer, RebusConfigurer> configure, bool startBus = true, bool enablePolymorphicDispatch = false)
        {
            if (containerBuilder == null) throw new ArgumentNullException(nameof(containerBuilder));
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            new AutofacHandlerActivator(containerBuilder, (configurer, context) => configure(configurer), startBus, enablePolymorphicDispatch);
        }

        /// <summary>
        /// Makes the necessary registrations in the given <paramref name="containerBuilder"/>, invoking the
        /// <paramref name="configure"/> callback when Rebus needs to be configured.
        /// </summary>
        public static void RegisterRebus(this ContainerBuilder containerBuilder, Func<RebusConfigurer, IComponentContext, RebusConfigurer> configure, bool startBus = true, bool enablePolymorphicDispatch = false)
        {
            if (containerBuilder == null) throw new ArgumentNullException(nameof(containerBuilder));
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            new AutofacHandlerActivator(containerBuilder, (configurer, context) => configure(configurer, context), startBus, enablePolymorphicDispatch);
        }

        /// <summary>
        /// Registers all Rebus message handler types found in the assembly of <typeparamref name="T"/>
        /// </summary>
        public static void RegisterHandlersFromAssemblyOf<T>(this ContainerBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            builder.RegisterAssemblyTypes(typeof(T).Assembly)
                .Where(t => t.IsClass && !t.IsAbstract && t.GetInterfaces().Any(IsRebusHandler))
                .As(GetImplementedHandlerInterfaces)
                .InstancePerDependency()
                .PropertiesAutowired();
        }

        /// <summary>
        /// Registers the given type as a Rebus message handler
        /// </summary>
        public static void RegisterHandler<THandler>(this ContainerBuilder builder) where THandler : IHandleMessages
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            var implementedHandlerTypes = GetImplementedHandlerInterfaces(typeof(THandler)).ToArray();

            builder.RegisterType(typeof(THandler)).As(implementedHandlerTypes)
                .InstancePerDependency()
                .PropertiesAutowired();
        }

        static IEnumerable<Type> GetImplementedHandlerInterfaces(Type handlerType) => handlerType.GetInterfaces().Where(IsRebusHandler);

        static bool IsRebusHandler(Type i) => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IHandleMessages<>);
    }
}