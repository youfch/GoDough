# Dependency injection

- [What is registered for you](#what-is-registered-for-you)
- [Adding your own services](#adding-your-own-services)
- [Using services in node scripts](#using-services-in-node-scripts)
- [`[Inject]` semantics](#inject-semantics)
- [Service factories](#service-factories)
- [Mapping a service as singleton](#mapping-a-service-as-singleton)
- [Replacing the container](#replacing-the-container)

## What is registered for you

`AppHost.ConfigureServices` registers the framework's own service graph by default:

| Service | Notes |
|---|---|
| `AppHost` | the host instance itself |
| `IAppHostNodeProvider` | resolves the autoload node |
| `IGodotApi` | async scene/shader loading |
| `Dispatcher` | main-thread dispatcher (also registered as an `IOnProcess`) |
| `ComponentFactory` | component instantiation |
| `SceneManagementService<>` | open generic, resolved per scene enum |

All of these use `TryAdd` semantics: **register your own implementation first to override a default.**

If you prefer to register a subset explicitly, `AddGodotRuntimeUtilities()`,
`AddThreadingUtilities()` and `AddComponentFactory()` are idempotent and may be called at any time.

The built-in host graph is intentionally *not* framework-specific beyond this: everything is resolved
through `IServiceProvider`, so any container works (see [Replacing the container](#replacing-the-container)).

## Adding your own services

Override `ConfigureServices` and always call `base.ConfigureServices(services)` first:

```cs
using Godot;
using GoDough.Runtime;
using Microsoft.Extensions.DependencyInjection;

public sealed class GameAppHost : AppHost
{
    public GameAppHost(Node autoLoadNode) : base(autoLoadNode) { }

    public override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);

        services.AddSingleton<IGameState, GameState>();
        services.AddSingleton<MyService>();
    }
}
```

## Using services in node scripts

Godot owns node constructors, so injection is **member injection**. Annotate properties or fields with
`[Inject]` and call `WireNode()` in `_Ready`:

```cs
using Godot;
using GoDough.Composition.Attributes;
using GoDough.Composition.Extensions;

public partial class MyNode : Node
{
    [Inject] private MyService _service = null!;

    public override void _Ready()
    {
        this.WireNode();   // injects every [Inject] member
        _service.DoWork();
    }
}
```

## `[Inject]` semantics

| Usage | Meaning |
|---|---|
| `[Inject]` | required, unkeyed service |
| `[Inject("key")]` | required, keyed service |
| `[Inject(Required = false)]` | optional: skipped when missing, never throws |

- **Members:** public *and* private **properties and fields**.
- **Derived attributes:** attributes inheriting from `InjectAttribute` are matched too.
- **Failure:** a missing required service throws an `InvalidOperationException` naming the service,
  the member and the declaring type — it never silently leaves the member `null`.
- **Container-agnostic:** services are resolved from the current `AppHost` service provider.
- **Skipped members:** indexers, get-only properties and readonly fields are ignored, not an error.

```cs
[Inject] private IGreeter _greeter = null!;                            // required
[Inject("pirate")] private IGreeter _pirate = null!;                   // keyed
[Inject(Required = false)] private IOptionalService? _optional = null; // optional
```

## Service factories

Register a factory for a type that must be created on demand:

```cs
services.AddFactory<IListItem, ListItem>();
```

Then inject `Factory<T>` and call `Create()`:

```cs
[Inject] private Factory<IListItem> _itemFactory = null!;

var item = _itemFactory.Create();
```

## Mapping a service as singleton

`MapSingleton` registers an interface mapping that resolves the same singleton instance:

```cs
public override void ConfigureServices(IServiceCollection services)
{
    base.ConfigureServices(services);

    services
        .AddSingleton<UserBootstrapper>()
        .MapSingleton<IBootstrapper, UserBootstrapper>();
}
```

## Replacing the container

Override `ConfigureIHostBuilder` to configure the underlying `IHostBuilder` **before** logging and
services are applied. This is where you replace the DI container or wire a host-level logger:

```cs
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public override IHostBuilder ConfigureIHostBuilder(IHostBuilder hostBuilder) =>
    hostBuilder
        .UseServiceProviderFactory(new AutofacServiceProviderFactory())
        .ConfigureContainer<ContainerBuilder>(container =>
        {
            // Autofac-native registration (not expressible as a service descriptor).
            container.RegisterType<Something>().As<ISomething>().SingleInstance();
        });
```

Your existing `ConfigureServices` registrations keep working: they are forwarded into the custom
container.

> Use `Autofac.Extensions.DependencyInjection` **9.0.0+**. Adapter 8.0.0 (Autofac 6.4.0) predates .NET 8
> keyed services: registering any `AddKeyed*` service throws `ArgumentNullException` while the host is
> being built, and keyed `[Inject("key")]` cannot resolve. Verified working with `11.0.2`.