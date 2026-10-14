using Godot;

using GoDough.Runtime;

namespace GoDough.Demo;

/// <summary>
/// Autoload 节点：唯一被挂到「自动加载」的东西。
/// AppHost 是普通 C# 类（非 Node），不能直接 autoload，所以由这个 Node 承载它，
/// 并把引擎回调转发给 AppHost。
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