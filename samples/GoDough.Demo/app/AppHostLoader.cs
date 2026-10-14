using Godot;

using GoDough.Runtime;

namespace GoDough.Demo;

/// <summary>
/// Autoload node: the only thing registered under Project Settings &gt; Autoload.
/// AppHost is a plain C# class (not a Node), so it cannot be autoloaded directly;
/// this node hosts it and forwards the engine callbacks to it.
/// </summary>
public partial class AppHostLoader : Node
{
    private GameAppHost _appHost = null!;

    public override void _Ready()
    {
        _appHost = new GameAppHost(this);
        _appHost.Start();
    }

    public override void _Process(double delta) => _appHost.Process(delta);

    public override void _PhysicsProcess(double delta) => _appHost.PhysicsProcess(delta);

    public override void _Input(InputEvent ev) => _appHost.Input(ev);

    public override void _UnhandledInput(InputEvent ev) => _appHost.UnhandledInput(ev);
}