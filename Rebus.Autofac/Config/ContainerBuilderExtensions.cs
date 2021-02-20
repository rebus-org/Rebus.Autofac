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
        /// <paramref name="configure"/> callback when Rebus needs to be configured. You can only have a single
        /// bus and set of handlers registered within an Autofac IoC container. If you wish to have multiple handlers, you will need
        /// to split up the bus into a one way bus, and multiple handler registrations.
        /// </summary>
        public static void RegisterRebus(this ContainerBuilder containerBuilder, Func<RebusConfigurer, RebusConfigurer> configure, bool startBus = true, bool enablePolymorphicDispatch = false)
        {
            if (containerBuilder == null) throw new ArgumentNullException(nameof(containerBuilder));
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            new AutofacHandlerActivator(containerBuilder, (configurer, context) => configure(configurer), startBus, enablePolymorphicDispatch);
        }

        /// <summary>
        /// Makes the necessary registrations in the given <paramref name="containerBuilder"/>, invoking the
        /// <paramref name="configure"/> callback when Rebus needs to be configured. You can only have a single
        /// bus and set of handlers registered within an Autofac IoC container. If you wish to have multiple handlers, you will need
        /// to split up the bus into a one way bus, and multiple handler registrations.
        /// </summary>
        public static void RegisterRebus(this ContainerBuilder containerBuilder, Func<RebusConfigurer, IComponentContext, RebusConfigurer> configure, bool startBus = true, bool enablePolymorphicDispatch = false)
        {
            if (containerBuilder == null) throw new ArgumentNullException(nameof(containerBuilder));
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            new AutofacHandlerActivator(containerBuilder, (configurer, context) => configure(configurer, context), startBus, enablePolymorphicDispatch);
        }

        /// <summary>
        /// Makes the necessary registrations in the given <paramref name="containerBuilder"/>, invoking the
        /// <paramref name="configure"/> callback when Rebus needs to be configured as a one way bus. You can only have a single
        /// one way bus registered within an Autofac IoC container for sending messages.
        /// </summary>
        public static void RegisterOneWayRebus(this ContainerBuilder containerBuilder, Func<RebusConfigurer, RebusConfigurer> configure)
        {
            if (containerBuilder == null) throw new ArgumentNullException(nameof(containerBuilder));
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            new AutofacOneWayBusActivator(containerBuilder, (configurer, context) => configure(configurer));
        }

        /// <summary>
        /// Makes the necessary registrations in the given <paramref name="containerBuilder"/>, invoking the
        /// <paramref name="configure"/> callback when Rebus needs to be configured as a one way bus. You can only have a single
        /// one way bus registered within an Autofac IoC container for sending messages.
        /// </summary>
        public static void RegisterOneWayRebus(this ContainerBuilder containerBuilder, Func<RebusConfigurer, IComponentContext, RebusConfigurer> configure)
        {
            if (containerBuilder == null) throw new ArgumentNullException(nameof(containerBuilder));
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            new AutofacOneWayBusActivator(containerBuilder, (configurer, context) => configure(configurer, context));
        }

        /// <summary>
        /// Makes the necessary registrations in the given <paramref name="containerBuilder"/>, invoking the
        /// <paramref name="configure"/> callback when Rebus needs to be configured. You can only have a single
        /// bus and set of handlers registered within an Autofac IoC container. If you wish to have multiple handlers, you will need
        /// to split up the bus into a one way bus, and multiple handler registrations.
        /// </summary>
        public static void RegisterRebusMultipleHandlers<THandlerBase>(this ContainerBuilder containerBuilder, Func<RebusConfigurer, RebusConfigurer> configure, bool enablePolymorphicDispatch = false)
            where THandlerBase : class
        {
            if (containerBuilder == null) throw new ArgumentNullException(nameof(containerBuilder));
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            new AutofacMultipleHandlersActivator<THandlerBase>(containerBuilder, (configurer, context) => configure(configurer), enablePolymorphicDispatch);
        }

        /// <summary>
        /// Makes the necessary registrations in the given <paramref name="containerBuilder"/>, invoking the
        /// <paramref name="configure"/> callback when Rebus needs to be configured. You can only have a single
        /// bus and set of handlers registered within an Autofac IoC container. If you wish to have multiple handlers, you will need
        /// to split up the bus into a one way bus, and multiple handler registrations.
        /// </summary>
        public static void RegisterRebusMultipleHandlers<THandlerBase>(this ContainerBuilder containerBuilder, Func<RebusConfigurer, IComponentContext, RebusConfigurer> configure, bool enablePolymorphicDispatch = false)
            where THandlerBase : class
        {
            if (containerBuilder == null) throw new ArgumentNullException(nameof(containerBuilder));
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            new AutofacMultipleHandlersActivator<THandlerBase>(containerBuilder, (configurer, context) => configure(configurer, context), enablePolymorphicDispatch);
        }

        /// <summary>
        /// Registers all Rebus message handler types found in the assembly of <typeparamref name="T"/>
        /// </summary>
        public static void RegisterHandlersFromAssemblyOf<T>(this ContainerBuilder builder, PropertyWiringOptions propertyWiringOptions = PropertyWiringOptions.None)
        {
            RegisterHandlersFromAssemblyOf(builder, typeof(T), propertyWiringOptions);
        }

        /// <summary>
        /// Registers all Rebus message handler types found in the assembly of <paramref name="handlerType"/>
        /// </summary>
        public static void RegisterHandlersFromAssemblyOf(this ContainerBuilder builder, Type handlerType, PropertyWiringOptions propertyWiringOptions = PropertyWiringOptions.None)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (handlerType == null) throw new ArgumentNullException(nameof(handlerType));

            builder.RegisterAssemblyTypes(handlerType.Assembly)
                .Where(t => t.IsClass && !t.IsAbstract && t.GetInterfaces().Any(IsRebusHandler))
                .As(GetImplementedHandlerInterfaces)
                .InstancePerDependency()
                .PropertiesAutowired(propertyWiringOptions);
        }

        /// <summary>
        /// Registers all Rebus message handler types found in the assembly of <typeparamref name="T"/> under the namespace that type lives
        /// under. So all types within the same namespace will get mapped as handlers, but not types under other namespaces. This allows
        /// you to separate messages for specific queues by namespace and register them all in one go.
        /// </summary>
        public static void RegisterHandlersFromAssemblyNamespaceOf<T>(this ContainerBuilder builder, PropertyWiringOptions propertyWiringOptions = PropertyWiringOptions.None)
        {
            RegisterHandlersFromAssemblyNamespaceOf(builder, typeof(T), propertyWiringOptions);
        }

        /// <summary>
        /// Registers all Rebus message handler types found in the assembly of <paramref name="handlerType"/> under the namespace that type lives
        /// under. So all types within the same namespace will get mapped as handlers, but not types under other namespaces. This allows
        /// you to separate messages for specific queues by namespace and register them all in one go.
        /// </summary>
        public static void RegisterHandlersFromAssemblyNamespaceOf(this ContainerBuilder builder, Type handlerType, PropertyWiringOptions propertyWiringOptions = PropertyWiringOptions.None)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (handlerType == null) throw new ArgumentNullException(nameof(handlerType));

            builder.RegisterAssemblyTypes(handlerType.Assembly)
                .Where(t => t.IsClass && !t.IsAbstract && t.GetInterfaces().Any(IsRebusHandler) && t.Namespace != null && t.Namespace.StartsWith(handlerType.Namespace ?? string.Empty))
                .As(GetImplementedHandlerInterfaces)
                .InstancePerDependency()
                .PropertiesAutowired(propertyWiringOptions);
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
                .PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies);
        }

        static IEnumerable<Type> GetImplementedHandlerInterfaces(Type handlerType) => handlerType.GetInterfaces().Where(IsRebusHandler);

        static bool IsRebusHandler(Type i) => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IHandleMessages<>);
    }
}