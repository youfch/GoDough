using System;

using GoDough.Runtime;
using GoDough.Visuals;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GoDough.Demo;

/// <summary>
/// Bootstrapper: runs during host Boot(), which makes it a good place for startup-time
/// initialization (registering scenes, warming up services, ...).
/// </summary>
public sealed class DemoBootstrapper : IBootstrapper
{
    private readonly ILogger<DemoBootstrapper> _logger;
    private readonly IServiceProvider _services;
    private readonly IAutofacProbeService _autofacProbe;

    public DemoBootstrapper(
        ILogger<DemoBootstrapper> logger,
        IServiceProvider services,
        IAutofacProbeService autofacProbe)
    {
        _logger = logger;
        _services = services;
        _autofacProbe = autofacProbe;
    }

    public void Run()
    {
        // Proves the container really is Autofac.
        _logger.LogInformation("DI container implementation: {Container}", _services.GetType().FullName);
        _logger.LogInformation("Service registered through Autofac: {Probe}", _autofacProbe.Describe());

        // Register the scenes at startup. SceneManagementService is a singleton, so the
        // registrations stay valid for the lifetime of the app.
        var scenes = _services.GetRequiredService<SceneManagementService<SceneId>>();
        scenes.RegisterSceneFile(SceneId.Main, "res://scenes/Main.tscn");
        scenes.RegisterSceneFile(SceneId.Game, "res://scenes/Game.tscn");

        _logger.LogInformation("Demo bootstrap complete.");
    }
}