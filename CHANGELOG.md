# Changelog

## Unreleased

### Platform

- Target **.NET 8** and **Godot.NET.Sdk 4.7.2** (previously `net6.0` / `Godot.NET.Sdk 4.0.2`).
  `Microsoft.Extensions.*` dependencies moved to `8.0.x`. The library keeps using the Godot SDK, so
  `using Godot;` and the release output layout are unchanged.

### Added

- `AppHost.ConfigureIHostBuilder(IHostBuilder)`: a host-builder hook invoked before logging and
  services, enabling a custom DI container (for example Autofac via `UseServiceProviderFactory`) and
  host-level loggers (for example Serilog).
- Pluggable logging: `ConfigureLogging` composes `ILoggerProvider`s; `AddGodotLoggerByDefault`
  removes the default engine sink; `DefaultMinimumLogLevel` overrides the level. NLog / log4net plug
  in the usual ASP.NET Core way.
- `[Inject]` keyed services (`[Inject("key")]`) and optional injection (`[Inject(Required = false)]`);
  injection now also covers fields and non-public members.

### Fixed

- `[Inject]` attribute matching was reversed, so attributes derived from `InjectAttribute` were
  silently ignored. Derived attributes are now matched.
- Required members with no resolvable service now throw a descriptive error that names the member
  and type, instead of being silently set to `null`.
- `WireNode` no longer resets the stack trace when rethrowing.
