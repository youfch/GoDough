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
/// Main scene: demonstrates the four [Inject] variants and, on a separate pipeline,
/// [UniqueNode] node binding.
/// </summary>
public partial class Main : Control
{
    [Inject] private IGreeter _greeter = null!;                            // required - plain service
    [Inject("pirate")] private IGreeter _pirate = null!;                   // keyed service
    [Inject] private Counter _counter = null!;                             // singleton (also an IOnProcess)
    [Inject(Required = false)] private IOptionalService? _optional = null; // optional (unregistered - skipped)
    [Inject] private ILogger<Main> _logger = null!;                        // framework logger

    [UniqueNode("Label")] private Label _label = null!;                    // binds to %Label
    [UniqueNode("SwitchButton")] private Button _switchButton = null!;     // binds to %SwitchButton

    private string _greeting = "";

    public override void _Ready()
    {
        this.WireNode();        // service injection ([Inject])
        this.BindToViewModel(); // node binding ([UniqueNode]) - a separate pipeline

        _logger.LogInformation("Main scene ready");
        _logger.LogInformation("Optional service present: {Present}", _optional != null);

        // Call Greet only once here (it logs), so the output is not spammed every frame.
        _greeting = _greeter.Greet("Godot") + "\n" + _pirate.Greet("Godot");

        _switchButton.Pressed += OnSwitchPressed;
        UpdateLabel();
    }

    public override void _Process(double delta) => UpdateLabel();

    private void UpdateLabel() =>
        _label.Text = $"{_greeting}\n\nCounter.Frames = {_counter.Frames}\n\n(use the button below to switch scenes)";

    private void OnSwitchPressed() =>
        _ = AppHost.Instance!.Application.Services
            .GetRequiredService<SceneManagementService<SceneId>>()
            .LoadAndEnableScene(SceneId.Game);
}