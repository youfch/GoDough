using Autofac;
using Autofac.Extensions.DependencyInjection;

using Godot;

using GoDough.Runtime;
using GoDough.Runtime.LivecycleHooks;
using GoDough.Threading;
using GoDough.Visuals.Components;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using NLog;
using NLog.Extensions.Logging;

namespace GoDough.Demo;

/// <summary>
/// 应用宿主：演示「换容器（Autofac）+ 加日志（NLog）+ 注册业务服务」。
/// </summary>
public sealed class GameAppHost : AppHost
{
    public GameAppHost(Node autoLoadNode) : base(autoLoadNode) { }

    // (1) 宿主扩展点：在日志/服务之前介入，替换 DI 容器为 Autofac。
    public override IHostBuilder ConfigureIHostBuilder(IHostBuilder hostBuilder) =>
        hostBuilder
            .UseServiceProviderFactory(new AutofacServiceProviderFactory())
            .ConfigureContainer<ContainerBuilder>(container =>
            {
                // Autofac 原生注册（Microsoft.Extensions.DependencyInjection 的描述符表达不了）
                container
                    .RegisterType<AutofacProbeService>()
                    .As<IAutofacProbeService>()
                    .SingleInstance();
            });

    // (2) 日志可插拔：保留 GoDough 默认的引擎控制台 sink，再叠加 NLog。
    public override void ConfigureLogging(ILoggingBuilder loggingBuilder)
    {
        base.ConfigureLogging(loggingBuilder);

        // nlog.config 放在工程根，用 res:// 解析为真实文件路径（编辑器运行时有效）。
        // 注意：导出后 res:// 进 pck，需把 nlog.config 放到可执行文件旁，或改用代码配置。
        var configPath = ProjectSettings.GlobalizePath("res://nlog.config");
        if (System.IO.File.Exists(configPath))
        {
            LogManager.Setup().LoadConfigurationFromFile(configPath, optional: true);
        }

        loggingBuilder.AddNLog();
    }

    // (3) 服务注册。
    public override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);

        // 关键：GodotApi / ComponentFactory 的构造函数依赖 AppHost，必须注册。
        services.AddSingleton<AppHost>(this);

        // GoDough 可选工具集。
        services.AddGodotRuntimeUtilities(); // IGodotApi：异步加载场景/着色器
        services.AddThreadingUtilities();    // Dispatcher（作为 IOnProcess 被宿主驱动）
        services.AddComponentFactory();      // ComponentFactory

        // demo 业务服务。
        services.AddSingleton<IGreeter, Greeter>();
        services.AddKeyedSingleton<IGreeter, PirateGreeter>("pirate"); // 键控服务（Autofac 11 已支持）
        services.AddSingleton<Counter>();
        services.AddSingleton<IOnProcess>(sp => sp.GetRequiredService<Counter>());
        services.AddSingleton<IBootstrapper, DemoBootstrapper>();

        // 注意：IOptionalService 故意不注册，用于演示 [Inject(Required = false)]。
    }
}