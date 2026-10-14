using System;

using GoDough.Runtime;
using GoDough.Visuals;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GoDough.Demo;

/// <summary>
/// 引导器：宿主 Boot() 时执行，适合做「启动期」初始化（注册场景、预热服务等）。
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
        // 证明容器确实被换成了 Autofac。
        _logger.LogInformation("DI 容器实现: {Container}", _services.GetType().FullName);
        _logger.LogInformation("Autofac 原生注册的服务: {Probe}", _autofacProbe.Describe());

        // 启动期注册场景（SceneManagementService 是单例，注册后长期有效）。
        var scenes = _services.GetRequiredService<SceneManagementService<SceneId>>();
        scenes.RegisterSceneFile(SceneId.Main, "res://scenes/Main.tscn");
        scenes.RegisterSceneFile(SceneId.Game, "res://scenes/Game.tscn");

        _logger.LogInformation("Demo 引导完成。");
    }
}