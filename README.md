# Godot Utilities

Utility library for implementing state of the art application development in Godot 4. Includes:

- Using .NET Standards:
  - Logging setup via the `ILogger` interface (Microsoft.Extensions.Logging), pluggable with NLog, log4net, Serilog, ...
  - Dependency injection (Microsoft.Extensions.DependencyInjection), with a replaceable container (Autofac, ...)

- General utilities for Godot:
  - Node extension methods
  - `[Inject]` property/field service injection
  - Scene management

- Targets `.NET 8` and `Godot.NET.Sdk 4.7.2`

See [Wiki](https://github.com/Hobart2967/GoDough/wiki) for more documentation.

## Getting Started

Derive from `AppHost` and start it from an autoload node:

```cs
using Godot;
using GoDough.Runtime;

public class GameAppHost : AppHost {
  public GameAppHost(Node autoLoadNode) : base(autoLoadNode) { }
}

public partial class AppHostLoader : Node {
  private GameAppHost _appHost = null!;

  public override void _Ready() {
    _appHost = new GameAppHost(this);
    _appHost.Start();
  }

  public override void _Process(double delta) => _appHost.Process(delta);
  public override void _PhysicsProcess(double delta) => _appHost.PhysicsProcess(delta);
  public override void _Input(InputEvent ev) => _appHost.Input(ev);
  public override void _UnhandledInput(InputEvent ev) => _appHost.UnhandledInput(ev);
}
```

## Host builder hook

Override `ConfigureIHostBuilder` to configure the underlying `IHostBuilder` **before** logging and
services are applied. This is where you replace the DI container or wire a host-level logger such as
Serilog. Do not configure logging or services here; use `ConfigureLogging` / `ConfigureServices`.

```cs
public override IHostBuilder ConfigureIHostBuilder(IHostBuilder hostBuilder) =>
  hostBuilder.UseServiceProviderFactory(new AutofacServiceProviderFactory());
```

## Logging

By default the library registers an engine-console logger (`AddGodotLogger`). In debug builds the
minimum level defaults to `LogLevel.Trace`; override `DefaultMinimumLogLevel` to change it.

### Add NLog / log4net next to the default sink

`ConfigureLogging` composes `ILoggerProvider`s, exactly like ASP.NET Core. Keep the engine console
sink and add third-party providers (add the relevant package to your game project):

```cs
// NLog.Extensions.Logging
public override void ConfigureLogging(ILoggingBuilder loggingBuilder) {
  base.ConfigureLogging(loggingBuilder);   // keep the engine console sink
  loggingBuilder.AddNLog();                // nlog.config next to the application
}

// Microsoft.Extensions.Logging.Log4Net.AspNetCore
public override void ConfigureLogging(ILoggingBuilder loggingBuilder) {
  base.ConfigureLogging(loggingBuilder);
  loggingBuilder.AddLog4Net("log4net.config");
}
```

### Remove the default engine sink

```cs
protected override bool AddGodotLoggerByDefault => false;
```

### Replace the host logger (Serilog)

```cs
public override IHostBuilder ConfigureIHostBuilder(IHostBuilder hostBuilder) =>
  hostBuilder.UseSerilog((context, config) => config.WriteTo.File("log.txt"));
```

Filtering works from configuration (`Logging:LogLevel`, per category), as with a normal .NET host.

## Replace the dependency injection container

The framework resolves all of its own services only through `IServiceProvider`, so any container
that plugs into `IHostBuilder.UseServiceProviderFactory` works. With Autofac
(`Autofac.Extensions.DependencyInjection`):

> Use `Autofac.Extensions.DependencyInjection` **9.0.0+**. Older adapters (e.g. 8.0.0, which pulls in
> Autofac 6.x) predate .NET 8 keyed services: registering any `AddKeyed*` service throws
> `ArgumentNullException` while the host is being built, and `[Inject("key")]` cannot resolve.
> Verified working with `11.0.2`.

```cs
public override IHostBuilder ConfigureIHostBuilder(IHostBuilder hostBuilder) =>
  hostBuilder.UseServiceProviderFactory(new AutofacServiceProviderFactory());
```

Your existing registrations (`ConfigureServices`, `MapSingleton`, `AddFactory`, ...) keep working and
are forwarded into the custom container.

## `[Inject]` node injection

Annotate public or private properties and fields. Attributes derived from `[Inject]` are matched too.

```cs
using Godot;
using GoDough.Composition.Attributes;
using GoDough.Composition.Extensions;

public partial class PlayerNode : Node {
  [Inject] private IGameState _gameState = null!;              // private field
  [Inject] private IScoreService Score { get; set; } = null!;  // private property
  [Inject("audio")] private IAudioChannel _audio = null!;      // keyed service
  [Inject(Required = false)] private IOptionalService _opt = null!; // optional

  public override void _Ready() {
    this.WireNode();
  }
}
```

- Members are resolved from the current `AppHost` service provider, so injection is container-agnostic.
- Required (default) members throw a descriptive error when the service is missing; optional members
  are skipped. Keyed services use `[Inject("key")]`.

## Node binding (`[UniqueNode]` / `[FromNode]`)

Node binding is a **separate pipeline** from service injection: it resolves nodes from the scene tree
and never touches the service container. Call it alongside `WireNode` when you need both.

- `[UniqueNode]` resolves a **unique-named** node (`%`).
- `[FromNode]` takes a **verbatim Godot NodePath**: a child, a relative path, an absolute path, a
  unique name, or a parent step.

```cs
using Godot;
using GoDough.Composition.Extensions;   // WireNode
using GoDough.Visuals.Attributes;       // [UniqueNode] / [FromNode]
using GoDough.Visuals.Components;       // BindToViewModel

public partial class PlayerHud : Control {
  [Inject] private IGameState _gameState = null!;                  // service

  [UniqueNode] private Label _score = null!;                       // %score   (member name)
  [UniqueNode("HealthBar")] private ProgressBar _health = null!;   // %HealthBar

  [FromNode("Title")] private Label _title = null!;                // child "Title" (not unique)
  [FromNode("Hud/Score")] private Label _hudScore = null!;         // relative path
  [FromNode("/root/Main/Hud/Total")] private Label _total = null!; // absolute path
  [FromNode("..")] private Control _parent = null!;                // parent
  [FromNode(ignoreInexistent: true)] private Label _optional = null!; // missing - skipped

  public override void _Ready() {
    this.WireNode();        // services
    this.BindToViewModel(); // nodes
  }
}
```

- When no name/path is given, the member name is used and a **leading underscore is stripped**
  (`_score` → `score`), so `[UniqueNode] private Label _score` binds to `%score`.
- Public and private properties and fields are bound; indexers, get-only properties and readonly
  fields are skipped.
- A missing node throws `NodeNotFoundException` (naming the path, the member and its owner); annotate
  the member with `ignoreInexistent: true` to skip it instead.
- A node whose runtime type does not match the member throws `NodeBindingException`, reporting the
  member name, the expected type and the actual type.
- Godot node names are **case-sensitive** (`%score` and `%Score` differ), so prefer unique names or
  relative paths; absolute paths (`/root/...`) are stable references but depend on the scene tree.

## Default service registration

`AppHost.ConfigureServices` registers the framework's own service graph out of the box — the host
itself, `IGodotApi`, the main-thread dispatcher, the component factory, and scene management with its
dependencies — so they resolve without any extra wiring. Every registration uses `TryAdd` semantics:
register your own implementation first to override a default. If you prefer to register a subset
explicitly, `AddGodotRuntimeUtilities()`, `AddThreadingUtilities()` and `AddComponentFactory()` are
idempotent and can be called at any time.
