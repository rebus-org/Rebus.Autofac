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
    public class NewAutofacActivationContext : IActivationContext
    {
        public IHandlerActivator CreateActivator(Action<IHandlerRegistry> configureHandlers, out IActivatedContainer container)
        {
            var builder = new ContainerBuilder();
            configureHandlers(new HandlerRegistry(builder));

            var containerAdapter = new AutofacContainerAdapter2(builder, startBus: false);

            var autofacContainer = builder.Build();
            container = new ActivatedContainer(autofacContainer);

            return containerAdapter;
        }

        public IBus CreateBus(Action<IHandlerRegistry> configureHandlers, Func<RebusConfigurer, RebusConfigurer> configureBus, out IActivatedContainer container)
        {
            var containerBuilder = new ContainerBuilder();
            configureHandlers(new HandlerRegistry(containerBuilder));

            var containerAdapter = new AutofacContainerAdapter2(containerBuilder);

            var autofacContainer = containerBuilder.Build();
            container = new ActivatedContainer(autofacContainer);

            return configureBus(Configure.With(containerAdapter)).Start();
        }

        class HandlerRegistry : IHandlerRegistry
        {
            readonly ContainerBuilder _containerBuilder;

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

        class ActivatedContainer : IActivatedContainer
        {
            readonly IContainer _container;

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