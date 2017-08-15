using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Tests.Contracts.Activation;

namespace Rebus.Autofac.Tests
{
    public class AutofacActivationContext : IActivationContext
    {
        public IHandlerActivator CreateActivator(Action<IHandlerRegistry> configureHandlers, out IActivatedContainer container)
        {
            var containerBuilder = new ContainerBuilder();
            configureHandlers(new HandlerRegistry(containerBuilder));

            var autoFacContainer = containerBuilder.Build();
            container = new ActivatedContainer(autoFacContainer);

            return new AutofacContainerAdapter(autoFacContainer);
        }

        public IBus CreateBus(Action<IHandlerRegistry> configureHandlers, Func<RebusConfigurer, RebusConfigurer> configureBus, out IActivatedContainer container)
        {
            var containerBuilder = new ContainerBuilder();
            configureHandlers(new HandlerRegistry(containerBuilder));

            var autoFacContainer = containerBuilder.Build();
            container = new ActivatedContainer(autoFacContainer);
            
            return configureBus(Configure.With(new AutofacContainerAdapter(autoFacContainer))).Start();
        }

        private class HandlerRegistry : IHandlerRegistry
        {
            private readonly ContainerBuilder _containerBuilder;

            public HandlerRegistry(ContainerBuilder containerBuilder)
            {
                _containerBuilder = containerBuilder;
            }

            public IHandlerRegistry Register<THandler>() where THandler : class, IHandleMessages
            {
                _containerBuilder.RegisterType<THandler>()
                    .As(GetHandlerInterfaces<THandler>().ToArray())
                    .InstancePerDependency();

                return this;
            }

            static IEnumerable<Type> GetHandlerInterfaces<THandler>() where THandler : class, IHandleMessages
            {
                return typeof(THandler)
                    .GetInterfaces()
                    .Where(i => i.HasGenericTypeDefinition(typeof(IHandleMessages<>)));
            }
        }

        private class ActivatedContainer : IActivatedContainer
        {
            private readonly IContainer _container;

            public ActivatedContainer(IContainer container)
            {
                _container = container;
            }

            public void Dispose()
            {
                _container.Dispose();
            }

            public IBus ResolveBus()
            {
                return _container.Resolve<IBus>();
            }
        }
    }
}