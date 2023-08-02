# Changelog

## 2.0.0-a1
* Update Autofac dependency to 4.1.0

## 2.0.0-a2
* Test build

## 2.0.0-b01
* Test release

## 2.0.0
* Release 2.0.0

## 3.0.0
* Update to Rebus 3

## 4.0.0
* Update to Rebus 4
* Add .NET Core support (netstandard1.6)
* Fix csproj - thanks [robvanpamel]
* Update Autofac dep to 4.5.0
* Update contracts dep - thanks [trevorreeves]

## 5.0.0
* Change API to work better with the Autofac container builder - no more `.Update` :)

## 5.1.0
* Additional `RegisterRebus` overload that passes `IComponentContext` to the configuration method

## 5.2.0
* Add Rebus handler registration extensions on `ContainerBuilder` and improve resolution performance

## 6.0.0
* Move polymorphic handler resolution resposiblity to the container. If contravariant lookup is wanted, one must register `ContravariantRegistrationSource` on the `ContainerBuilder`
* Update to Rebus 5 and Autofac 5
* Fix dispatch of `IFailed<TMessage>` when 2nd level retry is enabled - thanks [oliverhanappi]
* Fix subtle issues and make implementation that fixes dispatch of `IFailed<TMessage>` more elegant - thanks [oliverhanappi]
* Registration helpers should only register handlers under their implemented handler interfaces
* Fix resolution-during-startup race condition bug - thanks [leomenca]
* Fix bug that would erronously dispatch 2nd level retries twice

## 7.0.0
* Update to Rebus 6

## 7.1.0
* Remove CLS compliance attribute accidentally left over from the past

## 7.2.0
* Additional registration overloads - thanks [kendallb]

## 7.3.0
* Move multiple registrations check to the builder's registration callback, because it doesn't interfere with the container's `IBus` registration. This way, `IBus` can be overridden/decorated if desired. The check can also be disabled alltogether by passing `disableMultipleRegistrationsCheck: true` to the `RegisterRebus` method.

## 8.0.0
* Rename `startBus` to `startAutomatically` to be consistent with other modern containers
* Add container extension method `container.StartBus()` to make starting the bus more convenient

## 9.0.0-alpha02
* Update to Rebus 8
* Update to Autofac 7.0.1

[kendallb]: https://github.com/kendallb
[leomenca]: https://github.com/leomenca
[oliverhanappi]: https://github.com/oliverhanappi
[robvanpamel]: https://github.com/robvanpamel
[trevorreeves]: https://github.com/trevorreeves
