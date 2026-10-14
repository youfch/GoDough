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
/// Main scene: demonstrates the four [Inject] variants and, on a separate pipeline, both node
/// binding forms — [UniqueNode] (unique names) and [FromNode] (verbatim NodePath expressions).
/// </summary>
public partial class Main : Control
{
    // --- service injection ([Inject]) ---
    [Inject] private IGreeter _greeter = null!;                            // required - plain service
    [Inject("pirate")] private IGreeter _pirate = null!;                   // keyed service
    [Inject] private Counter _counter = null!;                             // singleton (also an IOnProcess)
    [Inject(Required = false)] private IOptionalService? _optional = null; // optional (unregistered - skipped)
    [Inject] private ILogger<Main> _logger = null!;                        // framework logger

    // --- node binding: [UniqueNode] resolves a unique-named (%) node ---
    [UniqueNode("Label")] private Label _label = null!;   // explicit name    -> %Label
    [UniqueNode] private Label _status = null!;           // member "_status" -> %status (leading underscore stripped)

    // --- node binding: [FromNode] resolves a verbatim NodePath ---
    [FromNode("SwitchButton")] private Button _switchButton = null!;           // child (not unique)
    [FromNode("Info/Title")] private Label _title = null!;                     // relative path
    [FromNode("/root/Main/Info/Title")] private Label _absoluteTitle = null!;  // absolute path
    [FromNode(ignoreInexistent: true)] private Label _missing = null!;         // missing -> skipped

    private string _greeting = "";

    public override void _Ready()
    {
        this.WireNode();        // service injection ([Inject])
        this.BindToViewModel(); // node binding ([UniqueNode] / [FromNode]) - a separate pipeline

        _logger.LogInformation("Main scene ready");
        _logger.LogInformation("Optional service present: {Present}", _optional != null);

        // Proves which nodes each binding form resolved.
        _logger.LogInformation(
            "Bindings -> %Label={Label}, %status={Status}, child={Child}, relative={Relative}, absolute={Absolute}, missing={Missing}",
            _label.Name,
            _status.Name,
            _switchButton.Name,
            _title.Name,
            _absoluteTitle.Name,
            _missing == null ? "<skipped>" : _missing.Name);

        _title.Text = "bound via [FromNode(\"Info/Title\")]";
        _absoluteTitle.Text = "bound via [FromNode(\"/root/Main/Info/Title\")]";
        _status.Text = "bound via [UniqueNode] on _status (underscore stripped)";

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