# Threading

- [Why a dispatcher](#why-a-dispatcher)
- [Using the dispatcher](#using-the-dispatcher)
- [Name collision with `Godot.Dispatcher`](#name-collision-with-godotdispatcher)

## Why a dispatcher

Threads other than Godot's main/UI thread may not modify UI elements. When you `await` work on a
background thread and then need to touch the scene tree, you have to "jump back" to the main thread.

`GoDough.Threading.Dispatcher` does that: `Invoke` queues a delegate that is executed on the main
thread during the next frame.

## Using the dispatcher

The dispatcher is **registered by default** as part of the host's service graph (and mapped to
`IOnProcess` so its queue is drained every frame). `AddThreadingUtilities()` is therefore optional;
calling it is idempotent.

Inject it and queue work:

```cs
using Godot;
using GoDough.Runtime;
using GoDough.Visuals;
using System;
using System.Threading.Tasks;

public sealed class ModelRegistry
{
    private readonly IGodotApi _godotApi;
    private readonly GoDough.Threading.Dispatcher _dispatcher;

    public ModelRegistry(IGodotApi godotApi, GoDough.Threading.Dispatcher dispatcher) =>
        (_godotApi, _dispatcher) = (godotApi, dispatcher);

    public async Task<Node3D> LoadModelAsync(string file)
    {
        var scene = await _godotApi.LoadSceneAsync("res://scenes/my-scene.tscn");

        // Instantiate on the main thread.
        var node = await _dispatcher.Invoke(() => scene.Instantiate<Node3D>());

        return node;
    }
}
```

For fire-and-forget work use the `Action` overload: `_dispatcher.Invoke(() => ...)`.

## Name collision with `Godot.Dispatcher`

Godot ships a `Dispatcher` type as well, so a file that imports both `Godot` and `GoDough.Threading`
cannot use `Dispatcher` unqualified. Either fully qualify it
(`GoDough.Threading.Dispatcher`) or alias it:

```cs
using Dispatcher = GoDough.Threading.Dispatcher;
```