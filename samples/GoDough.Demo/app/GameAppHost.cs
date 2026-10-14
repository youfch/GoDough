using Autofac;
using Autofac.Extensions.DependencyInjection;

using Godot;

using GoDough.Runtime;
using GoDough.Runtime.LivecycleHooks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using NLog;
using NLog.Extensions.Logging;

namespace GoDough.Demo;

/// <summary>
/// Application host: demonstrates replacing the DI container (Autofac), adding a logging
/// provider (NLog) and registering the application services.
/// </summary>
public sealed class GameAppHost : AppHost
{
    public GameAppHost(Node autoLoadNode) : base(autoLoadNode) { }

    // (1) Host extension point: runs before logging/services are applied, so this is where
    //     the DI container is replaced with Autofac.
    public override IHostBuilder ConfigureIHostBuilder(IHostBuilder hostBuilder) =>
        hostBuilder
            .UseServiceProviderFactory(new AutofacServiceProviderFactory())
            .ConfigureContainer<ContainerBuilder>(container =>
            {
                // Autofac-native registration (not expressible with a
                // Microsoft.Extensions.DependencyInjection service descriptor).
                container
                    .RegisterType<AutofacProbeService>()
                    .As<IAutofacProbeService>()
                    .SingleInstance();
            });

    // (2) Pluggable logging: keep GoDough's default engine console sink and add NLog on top.
    public override void ConfigureLogging(ILoggingBuilder loggingBuilder)
    {
        base.ConfigureLogging(loggingBuilder);

        // nlog.config lives in the project root and is resolved through res:// to a real file
        // path (works when running from the editor). Note: after export res:// lives inside the
        // .pck, so place nlog.config next to the executable, or configure NLog in code instead.
        var configPath = ProjectSettings.GlobalizePath("res://nlog.config");
        if (System.IO.File.Exists(configPath))
        {
            LogManager.Setup().LoadConfigurationFromFile(configPath, optional: true);
        }

        loggingBuilder.AddNLog();
    }

    // (3) Service registration. The host already registers its own service graph by default
    //     (AppHost, IGodotApi, Dispatcher, ComponentFactory), so only application services
    //     are registered here.
    public override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);

        // Demo services.
        services.AddSingleton<IGreeter, Greeter>();
        services.AddKeyedSingleton<IGreeter, PirateGreeter>("pirate"); // keyed service; needs Autofac 9.0.0+
        services.AddSingleton<Counter>();
        services.AddSingleton<IOnProcess>(sp => sp.GetRequiredService<Counter>());
        services.AddSingleton<IBootstrapper, DemoBootstrapper>();

        // Note: IOptionalService is intentionally left unregistered, to demonstrate
        // [Inject(Required = false)].
    }
}