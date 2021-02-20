using System;
using System.Collections.Concurrent;
using Autofac.Core;
using Rebus.Bus;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using Rebus.Handlers;
using Rebus.Internals;
using Rebus.Retry.Simple;
using Rebus.Transport;
// ReSharper disable ArgumentsStyleLiteral
#pragma warning disable 1998

namespace Rebus.Autofac
{
    internal static class AutofacHelpers
    {
        /// <summary>
        /// Returns true if the IBus interface has been registered multiple times, which is bad
        /// </summary>
        /// <param name="registrations">Registrations to check</param>
        /// <returns>True if registered multiple times</returns>
        public static bool HasMultipleBusRegistrations(IEnumerable<IComponentRegistration> registrations) =>
            registrations.SelectMany(r => r.Services)
                .OfType<TypedService>()
                .Count(s => s.ServiceType == typeof(IBus)) > 1;

        /// <summary>
        /// Resolves all the handlers for a particular message type via Autofac
        /// </summary>
        /// <param name="transactionContext">Transaction context for the handler</param>
        /// <param name="container">Autofac container for creating a new lifetime scope in a new transaction context</param>
        /// <param name="resolvers">Cache of resolves to cache and look them up in</param>
        /// <param name="handlerBaseType">Base type of handler to expect, null to allow them all</param>
        /// <typeparam name="TMessage">Type of message we want to handle</typeparam>
        /// <returns>Enumeration of resolved handlers for this message type</returns>
        public static IEnumerable<IHandleMessages<TMessage>> ResolveAutofacHandlers<TMessage>(ITransactionContext transactionContext, ILifetimeScope container,
            ConcurrentDictionary<Type, Func<ILifetimeScope, IEnumerable<IHandleMessages>>> resolvers, Type handlerBaseType)
        {
            // Get the resolver for creating the handlers for this type from the cache, and create it if not present
            var resolver = resolvers.GetOrAdd(typeof(TMessage), messageType => CreateResolveEnumeration<TMessage>(handlerBaseType));

            // Create a new lifetime scope for this handler if required in the transaction context
            var lifetimeScope = transactionContext.GetOrAdd("current-autofac-lifetime-scope", () => CreateLifetimeScope(transactionContext, container));

            // Now run the resolver for this lifetime scope to create the handlers injected via Autofac for a particular lifetime scope
            return (IEnumerable<IHandleMessages<TMessage>>) resolver(lifetimeScope);
        }

        /// <summary>
        /// Creates the resolver enumeration to generate a list of handlers for a specific message type
        /// </summary>
        /// <param name="handlerBaseType">Base type of handler to expect, null to allow them all</param>
        /// <typeparam name="TMessage">Type of message we want to handle</typeparam>
        /// <returns>Enumeration of resolved handlers for this message type</returns>
        private static Func<ILifetimeScope, IEnumerable<IHandleMessages>> CreateResolveEnumeration<TMessage>(Type handlerBaseType)
        {
            // If this message is not derived from the base class we expect, then return no handlers
            var messageType = typeof(TMessage);
            if (handlerBaseType != null && !messageType.IsAssignableTo(handlerBaseType))
            {
                return scope => new IHandleMessages<TMessage>[0];
            }

            if (messageType.IsAssignableTo(typeof(IFailed<>)))
            {
                var containedMessageType = messageType.GetGenericTypeParameters(typeof(IFailed<>)).Single();
                var additionalTypesToResolveHandlersFor = containedMessageType.GetBaseTypes(includeSelf: false);
                var typesToResolve = new[] {containedMessageType}
                    .Concat(additionalTypesToResolveHandlersFor)
                    .Select(type => typeof(IEnumerable<>).MakeGenericType(typeof(IHandleMessages<>).MakeGenericType(typeof(IFailed<>).MakeGenericType(type))))
                    .ToArray();

                return scope =>
                {
                    var handlers = new List<IHandleMessages<TMessage>>();

                    foreach (var type in typesToResolve)
                    {
                        handlers.AddRange((IEnumerable<IHandleMessages<TMessage>>) scope.Resolve(type));
                    }

                    return handlers;
                };
            }

            return scope => scope.Resolve<IEnumerable<IHandleMessages<TMessage>>>();
        }

        /// <summary>
        /// Creates a new lifetime scope within the transaction context, and registers it to be disposed of when the transaction scope is disposed
        /// </summary>
        /// <param name="transactionContext">Transaction context for the handler</param>
        /// <param name="container">Autofac container for creating a new lifetime scope in a new transaction context</param>
        /// <returns>Lifetime scope to use for this transaction scope</returns>
        private static ILifetimeScope CreateLifetimeScope(ITransactionContext transactionContext, ILifetimeScope container)
        {
            var scope = container.BeginLifetimeScope();
            transactionContext.OnDisposed(ctx => scope.Dispose());
            return scope;
        }
    }
}
