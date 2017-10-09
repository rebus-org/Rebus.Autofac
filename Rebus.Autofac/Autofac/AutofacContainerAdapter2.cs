using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using Rebus.Activation;
using Rebus.Handlers;
using Rebus.Transport;

namespace Rebus.Autofac
{
    class AutofacContainerAdapter2 : IHandlerActivator
    {
        IContainer _container;

        public async Task<IEnumerable<IHandleMessages<TMessage>>> GetHandlers<TMessage>(TMessage message, ITransactionContext transactionContext)
        {
            if (_container == null) throw new InvalidOperationException("This container adapter instance has not had its SetContainer method called");

            throw new NotImplementedException();
        }

        public void SetContainer(IContainer container)
        {
            if (_container != null)
            {
                throw new InvalidOperationException("One container instance can only have its SetContainer method called once");
            }
            _container = container ?? throw new ArgumentNullException(nameof(container), "Please pass a container instance when calling this method");
        }
    }
}