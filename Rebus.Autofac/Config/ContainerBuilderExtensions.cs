using System;
using Autofac;
using Rebus.Autofac;
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

            new AutofacContainerAdapter2(containerBuilder, configurer => configure(configurer));
        }
    }
}