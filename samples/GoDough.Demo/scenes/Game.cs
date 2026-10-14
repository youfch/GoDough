using Godot;

using GoDough.Composition.Attributes;
using GoDough.Composition.Extensions;
using GoDough.Runtime;
using GoDough.Visuals;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GoDough.Demo;

/// <summary>
/// 第二个场景：证明场景切换后，Autoload 上的 AppHost / 容器 依然存活，注照样有效。
/// </summary>
public partial class Game : Control
{
    [Inject] private IGreeter _greeter = null!;
    [Inject] private ILogger<Game> _logger = null!;

    public override void _Ready()
    {
        this.WireNode();

        _logger.LogInformation("Game 场景就绪（场景切换成功）");

        GetNode<Label>("%Label").Text = _greeter.Greet("Game 场景");
        GetNode<Button>("%BackButton").Pressed += OnBackPressed;
    }

    private void OnBackPressed() =>
        _ = AppHost.Instance!.Application.Services
            .GetRequiredService<SceneManagementService<SceneId>>()
            .LoadAndEnableScene(SceneId.Main);
}