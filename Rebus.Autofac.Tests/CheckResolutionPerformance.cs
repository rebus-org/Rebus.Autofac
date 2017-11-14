using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using NUnit.Framework;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Transport;

namespace Rebus.Autofac.Tests
{
    [TestFixture]
    public class CheckResolutionPerformance
    {
        /*
         Initially:

            
Running 10 samples of 1000000 iterations
Running sample # 1
Running sample # 2
Running sample # 3
Running sample # 4
Running sample # 5
Running sample # 6
Running sample # 7
Running sample # 8
Running sample # 9
Running sample # 10
Results:

    5,088 s
    4,909 s
    4,677 s
    4,668 s
    4,524 s
    4,500 s
    4,477 s
    4,457 s
    4,466 s
    4,482 s

AVG: 4,625

            After caching types to resolve in ConcurrentDictionary:


Running 10 samples of 1000000 iterations
Running sample # 1
Running sample # 2
Running sample # 3
Running sample # 4
Running sample # 5
Running sample # 6
Running sample # 7
Running sample # 8
Running sample # 9
Running sample # 10
Results:

    4,312 s
    4,274 s
    4,282 s
    4,466 s
    4,450 s
    4,593 s
    4,291 s
    4,256 s
    4,548 s
    4,470 s

AVG: 4,394


            Refactor to local functions and collapsed typecast into one single cast:

            
Running 10 samples of 1000000 iterations
Running sample # 1
Running sample # 2
Running sample # 3
Running sample # 4
Running sample # 5
Running sample # 6
Running sample # 7
Running sample # 8
Running sample # 9
Running sample # 10
Results:

    3,906 s
    3,905 s
    3,911 s
    3,883 s
    3,899 s
    3,893 s
    4,668 s
    4,027 s
    4,017 s
    4,147 s

AVG: 4,026



             */
        [TestCase(10, 1000000, Ignore = "takes a long time")]
        [TestCase(10, 10000, Ignore = "takes a long time")]
        public async Task Run(int samples, int ops)
        {
            Console.WriteLine($"Running {samples} samples of {ops} iterations");

            var builder = new ContainerBuilder();

            builder.RegisterHandler<OrdinaryMessageHandler>();
            builder.RegisterHandler<PolymorphicMessageHandler>();
            builder.RegisterHandler<PolymorphicMessageHandler2>();

            var activator = new AutofacHandlerActivator(builder, (configurer, context) => { }, false);
            var timeSpans = new List<TimeSpan>();

            using (var container = builder.Build())
            {
                foreach (var sample in Enumerable.Range(1, samples))
                {
                    var elapsed = await TakeSample(sample, ops, container, activator);

                    timeSpans.Add(elapsed);
                }
            }

            Console.WriteLine($@"Results:

{string.Join(Environment.NewLine, timeSpans.Select(t => $"    {t.TotalSeconds:0.000} s"))}

AVG: {timeSpans.Select(t => t.TotalSeconds).Average():0.000}");
        }

        static async Task<TimeSpan> TakeSample(int sample, int ops, IContainer container, AutofacHandlerActivator activator)
        {
            Console.WriteLine($"Running sample # {sample}");

            var stopwatch = Stopwatch.StartNew();

            for (var counter = 0; counter < ops; counter++)
            {
                using (var scope = new RebusTransactionScope())
                {
                    var handlers1 = await activator.GetHandlers(new OrdinaryMessage(), scope.TransactionContext);
                }

                using (var scope = new RebusTransactionScope())
                {
                    var handlers2 = await activator.GetHandlers(new PolymorphicMessage(), scope.TransactionContext);
                }
            }

            return stopwatch.Elapsed;
        }

        class OrdinaryMessage { }

        class OrdinaryMessageHandler : IHandleMessages<OrdinaryMessage>
        {
            public Task Handle(OrdinaryMessage message)
            {
                throw new NotImplementedException();
            }
        }

        class PolymorphicMessage { }

        class PolymorphicMessageHandler : IHandleMessages<PolymorphicMessage>
        {
            public Task Handle(PolymorphicMessage message)
            {
                throw new NotImplementedException();
            }
        }

        class PolymorphicMessageHandler2 : IHandleMessages<object>
        {
            public Task Handle(object message)
            {
                throw new NotImplementedException();
            }
        }
    }
}