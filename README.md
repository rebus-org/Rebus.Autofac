# Rebus.Autofac

[![install from nuget](https://img.shields.io/nuget/v/Rebus.Autofac.svg?style=flat-square)](https://www.nuget.org/packages/Rebus.Autofac)

Provides an Autofac container adapter for [Rebus](https://github.com/rebus-org/Rebus).

![](https://raw.githubusercontent.com/rebus-org/Rebus/master/artwork/little_rebusbus2_copy-200x200.png)

---

Use the Autofac container adapter like this:

```csharp
var builder = new ContainerBuilder();

builder.RegisterRebus((configurer, context) => configurer
    .Logging(l => l.Serilog())
    .Transport(t => t.UseRabbitMq(...))
    .Options(o => {
        o.SetNumberOfWorkers(2);
        o.SetMaxParallelism(30);
    }));
    
// Register your handlers before building the container
builder.RegisterHandler<MyHandler>();

// the bus is registered now, but it has not been started.... make all your other registrations, and then:
var container = builder.Build(); //< start the bus

// now your application is running

// ALWAYS do this when your application shuts down:
container.Dispose();
```

It will automatically register the following services in Autofac:

* `IBus` – this is the bus singleton
* `IMessageContext` – this is the current message context – can be injected into Rebus message handlers and everything resolved at the time of receiving a new message
* `ISyncBus` – this is the synchronous bus instance – can be used in places, where a `Task`-based asynchronous API is not desired, e.g. from deep within ASP.NET or WPF applications, which would deadlock if you went `.Wait()` on a `Task`

Note that you cannot register multiple buses within a single container, so if you wish to use Autofac to inject dependencies into multiple busses, you will want to create separate container builders for each bus. Then you will have multiple threads servicing each bus pointing to a different message queue, and can send messages to them from another bus. So backgrounds threads can process separate queues for instance, and your main ASP.NET application can use a one-way bus to send messages to any of the other queues. Here is an example:

```csharp
// We need a common in memory network for this example
var inMemNetwork = new InMemNetwork();
const string firstQueueName = "first-queue";
const string secondQueueName = "second-queue";

// Set up a single instance of the event aggregator
var eventAggregator = new EventAggregator();

// Configure container for receiving the first message type and start it up
var firstBuilder = new ContainerBuilder();
firstBuilder.RegisterHandler<FirstHandler>();
firstBuilder.RegisterInstance(eventAggregator).SingleInstance();
firstBuilder.RegisterRebus(
    configurer => configurer
        .Transport(t => t.UseInMemoryTransport(inMemNetwork, firstQueueName))
        .Options(o =>
        {
            o.SetNumberOfWorkers(1);
            o.SetMaxParallelism(1);
        })
);
using var firstContainer = firstBuilder.Build();

// Configure container for receiving the second message type and start it up
var secondBuilder = new ContainerBuilder();
secondBuilder.RegisterHandler<SecondHandler>();
secondBuilder.RegisterInstance(eventAggregator).SingleInstance();
secondBuilder.RegisterRebus(
    configurer => configurer
        .Transport(t => t.UseInMemoryTransport(inMemNetwork, secondQueueName))
        .Options(o =>
        {
            o.SetNumberOfWorkers(1);
            o.SetMaxParallelism(1);
        })
);
using var secondContainer = secondBuilder.Build();

// Now configure the bus for sending messages. This will have no worker threads.
var sendBuilder = new ContainerBuilder();
sendBuilder.RegisterRebus(
    configurer => configurer
        .Transport(t => t.UseInMemoryTransportAsOneWayClient(inMemNetwork))
        .Routing(r => r.TypeBased()
            .Map<FirstMessage>(firstQueueName)
            .Map<SecondMessage>(secondQueueName))
);

using var sendContainer = sendBuilder.Build();
var bus = sendContainer.Resolve<IBus>();

await bus.Send(new FirstMessage("first message"));
await bus.Send(new SecondMessage("second message"));
```