using System;
using Autofac;

namespace Rebus.Config;

/// <summary>
/// Extensions for making it easier to work with Rebus
/// </summary>
public static class ContainerExtensions
{
    /// <summary>
    /// When Rebus is registered withg <code>startBus: false</code>, the bus can be started by calling the extension method
    /// </summary>
    public static void StartBus(this IContainer container)
    {
        if (container == null) throw new ArgumentNullException(nameof(container));

        container.Resolve<IBusStarter>().Start();
    }
}