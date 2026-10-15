# Getting started

- [Reference the library](#reference-the-library)
- [Create your AppHost](#create-your-apphost)
- [Register the autoload script](#register-the-autoload-script)
- [Access the host node from a service](#access-the-host-node-from-a-service)

## Reference the library

Add a reference to GoDough. The library targets `net8.0` and is built against
`Godot.NET.Sdk/4.7.2`, so your game project must use the same SDK version.

```xml
<Project Sdk="Godot.NET.Sdk/4.7.2">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <EnableDynamicLoading>true</EnableDynamicLoading>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <!-- Until a release containing the current API is published, reference the project directly. -->
    <ProjectReference Include="..\..\GoDough\src\GoDough.csproj" />

    <!-- Once a matching release exists:
    <PackageReference Include="GoDough" Version="1.2.0" />
    -->
  </ItemGroup>
</Project>
```

You do **not** need to reference `Microsoft.Extensions.Hosting` yourself — the library (and therefore
your game) gets it transitively.

> **Note:** the package ID is `GoDough`. (`Hobart2967.GoDough` is *not* published on NuGet.)

## Create your AppHost

`AppHost` is the central entry point, the same concept you may know from .NET hosting. Derive from it
and add whatever your game needs (services, logging, container).

```cs
using Godot;
using GoDough.Runtime;

public sealed class GameAppHost : AppHost
{
    public GameAppHost(Node autoLoadNode) : base(autoLoadNode) { }
}
```

`AppHost.ConfigureServices` already registers the framework's own service graph (the host itself,
`IGodotApi`, the main-thread `Dispatcher`, `ComponentFactory` and `SceneManagementService`), so you
only register your own services. See [Dependency injection](./dependency-injection.md).

## Register the autoload script

`AppHost` is a plain C# class, not a `Node`, so it cannot be autoloaded directly. Use a small
`Node` that hosts it:

1. `Project` → `Project Settings…` → `Globals` → `Autoload`
2. Add a C# script, e.g. `res://app/AppHostLoader.cs`, node name `App`

```cs
using Godot;
using GoDough.Runtime;

public partial class AppHostLoader : Node
{
    private GameAppHost _appHost = null!;

    public override void _Ready()
    {
        _appHost = new GameAppHost(this);
        _appHost.Start();
    }

    public override void _Process(double delta)         => _appHost.Process(delta);
    public override void _PhysicsProcess(double delta)  => _appHost.PhysicsProcess(delta);
    public override void _Input(InputEvent ev)          => _appHost.Input(ev);
    public override void _UnhandledInput(InputEvent ev) => _appHost.UnhandledInput(ev);
}
```

Forward **all** callbacks you want the host to drive. Lifecycle hooks
(`IOnProcess`, `IOnInput`, …) are only invoked for the callbacks you forward.

## Access the host node from a service

Inject `IAppHostNodeProvider` and call `GetNode()`:

```cs
using GoDough.Visuals;

public sealed class MyService
{
    private readonly IAppHostNodeProvider _nodes;

    public MyService(IAppHostNodeProvider nodes) => _nodes = nodes;

    public void DoSomething() => _nodes.GetNode().GetTree().ChangeSceneToFile("res://scenes/next.tscn");
}
```

## Next steps

- [Dependency injection](./dependency-injection.md) — services, `[Inject]`, replacing the container
- [Logging](./logging.md) — engine console sink, levels, NLog/log4net/Serilog
- [Node bindings](./node-bindings.md) — `[UniqueNode]` and `[FromNode]`