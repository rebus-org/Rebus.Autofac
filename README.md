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

If you wish to register multiple message queue handlers within a single container, you will need to split up the registration into handlers for each queue and a one way bus for sending messages. You will also need to set up the one way bus to use type based routing and register the appropriate queues for each type. 

That way you can have multiple threads servicing each bus pointing to a different message queue, and can send messages to them from another bus. So backgrounds threads can process separate queues for instance, and your main ASP.NET application can use a one-way bus to send messages to any of the other queues. 

Note however that the transport used for sending messages must be of the same type with named queues. So you cannot say send some messages to a memory queue and others to a SQL queue, they will all need to be stored in the same place.  

Here is an example:

```csharp
// We need a common in memory network for this test
var inMemNetwork = new InMemNetwork();
const string firstQueueName = "first-queue";
const string secondQueueName = "second-queue";

// Set up a single instance of the event aggregator and register it
var builder = new ContainerBuilder();
var eventAggregator = new EventAggregator();
builder.RegisterInstance(eventAggregator).SingleInstance();

// Configure handlers for receiving messages in the first queue and start it up
builder.RegisterRebusMultipleHandlers<FirstQueueMessageBase>(
    configurer => configurer
        .Transport(t => t.UseInMemoryTransport(inMemNetwork, firstQueueName))
        .Options(o =>
        {
            o.SetNumberOfWorkers(1);
            o.SetMaxParallelism(1);
        }));
builder.RegisterHandlersFromAssemblyNamespaceOf<FirstQueueMessageBase>();

// Configure container for receiving messages in the second queue and start it up
builder.RegisterRebusMultipleHandlers<SecondMessageQueueBase>(
    configurer => configurer
        .Transport(t => t.UseInMemoryTransport(inMemNetwork, secondQueueName))
        .Options(o =>
        {
            o.SetNumberOfWorkers(1);
            o.SetMaxParallelism(1);
        }));
builder.RegisterHandlersFromAssemblyNamespaceOf<SecondMessageQueueBase>();

// Now configure the bus for sending messages. This will have no worker threads and it's configured
// to send messages to the two separate queues separated by namespace.
builder.RegisterOneWayRebus(
    configurer => configurer
        .Transport(t => t.UseInMemoryTransportAsOneWayClient(inMemNetwork))
        .Routing(r => r.TypeBased()
            .MapAssemblyDerivedFrom<FirstQueueMessageBase>(firstQueueName)
            .MapAssemblyDerivedFrom<SecondMessageQueueBase>(secondQueueName))
);

// Build the container. No busses have started up yet, as we need to start them each
await using (_container = builder.Build())
{
    // Start up the event handlers
    var firstBusStarter = _container.Resolve<IBusStarter<FirstQueueMessageBase>>();
    firstBusStarter.Start();
    var secondBusStarter = _container.Resolve<IBusStarter<SecondMessageQueueBase>>();
    secondBusStarter.Start();

    // Now resolve the bus we can send messages with
    var bus = _container.Resolve<IBus>();
    await bus.Send(new FirstMessage("first message"));
    await bus.Send(new SecondMessage("second message"));
}
```