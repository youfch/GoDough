# Lifecycle hooks

Sometimes you want to react to the engine's frame cycle **without** writing a node. GoDough exposes
the frame callbacks as interfaces; implement any combination of them in a service.

> The namespace is spelled `GoDough.Runtime.LivecycleHooks` (sic).

| Interface | Callback |
|---|---|
| `IOnProcess` | `OnProcess(double delta)` |
| `IOnPhysicsProcess` | `OnPhysicsProcess(double delta)` |
| `IOnInput` | `OnInput(InputEvent ev)` |
| `IOnUnhandledInput` | `OnUnhandledInput(InputEvent ev)` |

Register the service and map it to the hook interface:

```cs
using GoDough.Runtime.LivecycleHooks;
using GoDough.Composition.Extensions;   // MapSingleton

public override void ConfigureServices(IServiceCollection services)
{
    base.ConfigureServices(services);

    services
        .AddSingleton<GameTimeService>()
        .MapSingleton<IOnProcess, GameTimeService>();
}
```

```cs
public sealed class GameTimeService : IOnProcess
{
    public float CurrentTicks { get; private set; }

    public void OnProcess(double delta) => CurrentTicks += (float)delta;
}
```

Multiple services can implement the same hook — all of them are invoked, in registration order. A
single service can implement several hooks:

```cs
public sealed class MyService : IOnProcess, IOnInput
{
    public void OnProcess(double delta) { /* ... */ }
    public void OnInput(InputEvent ev)  { /* ... */ }
}
```

Notes:

- Hooks only fire for the callbacks your **autoload** forwards to the host
  (`Process`, `PhysicsProcess`, `Input`, `UnhandledInput`). See
  [Getting started](./getting-started.md#register-the-autoload-script).
- The main-thread `Dispatcher` is itself registered as an `IOnProcess` by default, so the dispatcher
  queue is drained every frame without any extra registration.