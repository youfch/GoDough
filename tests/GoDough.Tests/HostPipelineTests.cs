using System.Collections.Generic;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using GoDough.Composition;
using GoDough.Composition.Attributes;
using GoDough.Composition.Extensions;
using GoDough.Runtime;
using GoDough.Runtime.LivecycleHooks;
using Godot;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GoDough.Tests;

internal interface IProbeService { }

internal sealed class ProbeService : IProbeService { }

internal interface IProbeWidget { }

internal sealed class ProbeWidget : IProbeWidget { }

internal sealed class ProbeHook : IOnProcess
{
    public void OnProcess(double delta) { }
}

internal sealed class ProbeBootstrapper : IBootstrapper
{
    public void Run() { }
}

internal sealed class RecordingAppHost : AppHost
{
    public RecordingAppHost(Node node) : base(node) { }

    public List<string> Order { get; } = new();

    public override IHostBuilder ConfigureIHostBuilder(IHostBuilder hostBuilder)
    {
        Order.Add("host");
        return base.ConfigureIHostBuilder(hostBuilder);
    }

    public override void ConfigureLogging(ILoggingBuilder loggingBuilder)
    {
        // Intentionally does not call base: the engine console default is verified separately and
        // OS.IsDebugBuild() requires the engine runtime.
        Order.Add("logging");
    }

    public override void ConfigureServices(IServiceCollection services)
    {
        Order.Add("services");
        base.ConfigureServices(services);
    }

    public IHostBuilder RunConfigureBuilder(IHostBuilder builder) => this.ConfigureBuilder(builder);
}

internal sealed class AutofacAppHost : AppHost
{
    public AutofacAppHost(Node node) : base(node) { }

    public override IHostBuilder ConfigureIHostBuilder(IHostBuilder hostBuilder) =>
        hostBuilder.UseServiceProviderFactory(new AutofacServiceProviderFactory());
}

internal sealed class KeyedWidgetTarget
{
    [Inject("keyed")] public IProbeWidget Widget { get; set; } = null!;
}

public class HostPipelineTests
{
    [Fact]
    public void Default_host_builder_hook_returns_builder_unchanged()
    {
        var host = new AppHost(null!);
        var builder = Host.CreateDefaultBuilder(null);

        Assert.Same(builder, host.ConfigureIHostBuilder(builder));
    }

    [Fact]
    public void Pipeline_invokes_host_hook_before_logging_and_services()
    {
        var host = new RecordingAppHost(null!);
        var builder = Host.CreateDefaultBuilder(null);

        using var app = host.RunConfigureBuilder(builder).Build();

        Assert.Equal(new[] { "host", "logging", "services" }, host.Order);
    }

    [Fact]
    public void Custom_service_provider_factory_is_used_and_resolves_services()
    {
        var host = new AutofacAppHost(null!);
        var builder = Host.CreateDefaultBuilder(null);

        host.ConfigureIHostBuilder(builder);
        builder.ConfigureServices(services => services.AddSingleton<IProbeService, ProbeService>());

        using var app = builder.Build();

        Assert.Contains("Autofac", app.Services.GetType().FullName);
        Assert.IsType<ProbeService>(app.Services.GetService<IProbeService>());
    }

    [Fact]
    public void Lifecycle_hooks_and_bootstrappers_resolve_under_custom_container()
    {
        var host = new AutofacAppHost(null!);
        var builder = Host.CreateDefaultBuilder(null);

        host.ConfigureIHostBuilder(builder);
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<ProbeHook>();
            services.AddSingleton<IOnProcess>(sp => sp.GetRequiredService<ProbeHook>());
            services.AddSingleton<ProbeBootstrapper>();
            services.AddSingleton<IBootstrapper>(sp => sp.GetRequiredService<ProbeBootstrapper>());
        });

        using var app = builder.Build();

        Assert.IsType<ProbeHook>(app.Services.GetService<IOnProcess>());
        Assert.Single(app.Services.GetServices<IOnProcess>());
        Assert.IsType<ProbeBootstrapper>(app.Services.GetService<IBootstrapper>());
    }

    [Fact]
    public void MapSingleton_and_AddFactory_work_under_custom_container()
    {
        var host = new AutofacAppHost(null!);
        var builder = Host.CreateDefaultBuilder(null);

        host.ConfigureIHostBuilder(builder);
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<ProbeService>();
            services.MapSingleton<IProbeService, ProbeService>();
            services.AddFactory<IProbeWidget, ProbeWidget>();
        });

        using var app = builder.Build();

        Assert.IsType<ProbeService>(app.Services.GetService<IProbeService>());

        var factory = app.Services.GetService<Factory<IProbeWidget>>();
        Assert.NotNull(factory);
        Assert.IsType<ProbeWidget>(factory!.Create());
    }

    [Fact]
    public void Keyed_services_resolve_under_custom_container()
    {
        // Guards against a regressing Autofac adapter that cannot translate .NET 8 keyed
        // service descriptors (adapter 8.0.0 / Autofac 6.4.0 throws during host build).
        var host = new AutofacAppHost(null!);
        var builder = Host.CreateDefaultBuilder(null);

        host.ConfigureIHostBuilder(builder);
        builder.ConfigureServices(services =>
            services.AddKeyedSingleton<IProbeService, ProbeService>("keyed"));

        using var app = builder.Build();

        Assert.IsAssignableFrom<IKeyedServiceProvider>(app.Services);
        Assert.IsType<ProbeService>(app.Services.GetRequiredKeyedService<IProbeService>("keyed"));
    }

    [Fact]
    public void Keyed_injection_works_under_custom_container()
    {
        var host = new AutofacAppHost(null!);
        var builder = Host.CreateDefaultBuilder(null);

        host.ConfigureIHostBuilder(builder);
        builder.ConfigureServices(services =>
            services.AddKeyedSingleton<IProbeWidget, ProbeWidget>("keyed"));

        using var app = builder.Build();

        var target = new KeyedWidgetTarget();
        NodeCompositionExtensions.WireMembers(target, app.Services);

        Assert.IsType<ProbeWidget>(target.Widget);
    }
}
