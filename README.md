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

// the bus is registered now, but it has not been started.... make all your other registrations, and then:
var container = builder.Build(); //< start the bus


// now your application is running


// ALWAYS do this when your application shuts down:
container.Dispose();
```

It will automatically register the following services in Autofac:

* `IBus` – this is the bus singleton
* `IBusStart` – this is the bus starter you can use to start it manually
* `IMessageContext` – this is the current message context – can be injected into Rebus message handlers and everything resolved at the time of receiving a new message
* `ISyncBus` – this is the synchronous bus instance – can be used in places, where a `Task`-based asynchronous API is not desired, e.g. from deep within ASP.NET or WPF applications, which would deadlock if you went `.Wait()` on a `Task`

Not that if you wish to regiser the bus and not start it automatically, you can stil register the number of workers but have it not start anything until you are ready for the bus to be started. This is usefu if you wish to separate the processing of messages from the sending of messages (separate tasks, services etc). You would do it like this:

```csharp
var builder = new ContainerBuilder();

builder.RegisterRebus((configurer, context) => configurer
    .Logging(l => l.Serilog())
    .Transport(t => t.UseRabbitMq(...))
    .Options(o => {
        o.SetNumberOfWorkers(2);
        o.SetMaxParallelism(30);
    }),
    false); // <--- here we tell it not to start the bus

// the bus is registered now, but it has not been started.... make all your other registrations, and then:
var container = builder.Build();
var busStarter = container.Resolve<IBusStarter>();
busStarter.Start(); //< start the bus


// now your application is running


// ALWAYS do this when your application shuts down:
container.Dispose();
```