using System;
using Autofac;
using Rebus.Bus;
using Rebus.Exceptions;

namespace Rebus.Config
{
    /// <summary>
    /// Autofac container extensions
    /// </summary>
    public static class ContainerExtensions
    {
        /// <summary>
        /// After having called <see cref="ContainerBuilderExtensions.AddRebus"/> followed by <see cref="ContainerBuilder.Build"/> on your <see cref="ContainerBuilder"/>,
        /// you may call this method to ensure that Rebus has been started.
        /// Please note that Rebus may already have been started if you resolved <see cref="IBus"/> already, or
        /// if the container resolved it while building another component.
        /// </summary>
        public static void UseRebus(this IContainer container)
        {
            try
            {
                container.Resolve<IBus>();
            }
            catch (Exception exception)
            {
                throw new RebusConfigurationException(exception, "Could not start Rebus");
            }
        }
    }
}