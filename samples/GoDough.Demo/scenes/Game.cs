using Godot;

using GoDough.Composition.Attributes;
using GoDough.Composition.Extensions;
using GoDough.Runtime;
using GoDough.Visuals;
using GoDough.Visuals.Attributes;
using GoDough.Visuals.Components;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GoDough.Demo;

/// <summary>
/// Second scene: proves that the AppHost / container living on the autoload survives a scene
/// change, that injection still works and that node binding is per-scene.
/// </summary>
public partial class Game : Control
{
    [Inject] private IGreeter _greeter = null!;
    [Inject] private ILogger<Game> _logger = null!;

    [UniqueNode("Label")] private Label _label = null!;                // binds to %Label
    [UniqueNode("BackButton")] private Button _backButton = null!;     // binds to %BackButton

    public override void _Ready()
    {
        this.WireNode();
        this.BindToViewModel();

        _logger.LogInformation("Game scene ready (scene switch worked)");

        _label.Text = _greeter.Greet("Game scene");
        _backButton.Pressed += OnBackPressed;
    }

    private void OnBackPressed() =>
        _ = AppHost.Instance!.Application.Services
            .GetRequiredService<SceneManagementService<SceneId>>()
            .LoadAndEnableScene(SceneId.Main);
}