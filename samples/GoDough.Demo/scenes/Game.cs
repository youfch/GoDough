using Godot;

using GoDough.Composition.Attributes;
using GoDough.Composition.Extensions;
using GoDough.Runtime;
using GoDough.Visuals;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GoDough.Demo;

/// <summary>
/// Second scene: proves that the AppHost / container living on the autoload survives a scene
/// change and that injection still works.
/// </summary>
public partial class Game : Control
{
    [Inject] private IGreeter _greeter = null!;
    [Inject] private ILogger<Game> _logger = null!;

    public override void _Ready()
    {
        this.WireNode();

        _logger.LogInformation("Game scene ready (scene switch worked)");

        GetNode<Label>("%Label").Text = _greeter.Greet("Game scene");
        GetNode<Button>("%BackButton").Pressed += OnBackPressed;
    }

    private void OnBackPressed() =>
        _ = AppHost.Instance!.Application.Services
            .GetRequiredService<SceneManagementService<SceneId>>()
            .LoadAndEnableScene(SceneId.Main);
}