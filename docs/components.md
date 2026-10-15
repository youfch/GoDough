# Godot components

Components let you follow a component-based architecture: a view (`*.tscn`) plus a bound view-model
class that holds references to the controls you need in code.

- [Component definition](#component-definition)
- [Registering the component](#registering-the-component)
- [Creating an instance](#creating-an-instance)

## Component definition

Design the view in a `*.tscn` file and link it to the component with `[SceneBinding]`. Declare the
controls you need as `[UniqueNode]` / `[FromNode]` members.

```cs
using Godot;
using GoDough.Visuals.Attributes;
using GoDough.Visuals.Components;

[SceneBinding("res://prefabs/my-component.tscn")]
public sealed class MyComponent : Component<Camera3D>   // generic = the scene root type; use Node if unsure
{
    [UniqueNode] public Button MyButton { get; set; } = null!;
}
```

When the component is created, the framework instantiates the scene, binds its members against the
scene root, and exposes the root through `SceneRoot`.

> `ComponentFactory` is registered by default, so you do not need `services.AddComponentFactory()`
> (calling it is harmless and idempotent).

## Registering the component

Components are resolved from the container, so register them:

```cs
public override void ConfigureServices(IServiceCollection services)
{
    base.ConfigureServices(services);

    services.AddTransient<MyComponent>();
}
```

## Creating an instance

Inject `ComponentFactory`, then create the component with the node type it is bound to:

```cs
using GoDough.Visuals.Components;

[Inject] private ComponentFactory _componentFactory = null!;

var component = _componentFactory.Create<MyComponent, Camera3D>();

var rootNode = component.SceneRoot;   // add it anywhere in the scene tree
AddChild(rootNode);
```

If you only have a `Type` at hand, use the non-generic overload:

```cs
var component = _componentFactory.Create(typeof(MyComponent));
```