using Godot;

using GoDough.Composition.Attributes;
using GoDough.Composition.Extensions;
using GoDough.Runtime;
using GoDough.Visuals;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GoDough.Demo;

/// <summary>
/// 主场景：集中演示 [Inject] 的四类语义。
/// </summary>
public partial class Main : Control
{
    [Inject] private IGreeter _greeter = null!;                          // 必填 · 普通服务
    [Inject("pirate")] private IGreeter _pirate = null!;                 // 键控服务
    [Inject] private Counter _counter = null!;                           // 单例（同时是 IOnProcess）
    [Inject(Required = false)] private IOptionalService? _optional = null; // 可选（未注册 → 跳过）
    [Inject] private ILogger<Main> _logger = null!;                      // 框架日志

    private Label _label = null!;
    private string _greeting = "";

    public override void _Ready()
    {
        this.WireNode(); // 注入所有 [Inject] 成员

        _label = GetNode<Label>("%Label");

        _logger.LogInformation("Main 场景就绪");
        _logger.LogInformation("可选服务是否存在: {Present}", _optional != null);

        // 只在这里调用一次 Greet（它会写日志），避免每帧刷屏。
        _greeting = _greeter.Greet("Godot") + "\n" + _pirate.Greet("Godot");

        GetNode<Button>("%SwitchButton").Pressed += OnSwitchPressed;
        UpdateLabel();
    }

    public override void _Process(double delta) => UpdateLabel();

    private void UpdateLabel() =>
        _label.Text = $"{_greeting}\n\nCounter.Frames = {_counter.Frames}\n\n（按下方按钮切换场景）";

    private void OnSwitchPressed() =>
        _ = AppHost.Instance!.Application.Services
            .GetRequiredService<SceneManagementService<SceneId>>()
            .LoadAndEnableScene(SceneId.Game);
}