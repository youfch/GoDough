using System.Linq;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Godot;
using GoDough.Runtime.LivecycleHooks;
using GoDough.Diagnostics.Logging;
using GoDough.Visuals;
using System;

namespace GoDough.Runtime
{
  public class AppHost
  {
    #region Private Fields
    public Node AutoLoadNode { get; private set; }
    #endregion

    public const string NodePath = "/root/DependencyInjection";

    #region Properties
    public IHost Application { get; private set; }
    public delegate void ProcessEventHandler(object sender, double delta);
    public delegate void InputEventHandler(object sender, InputEvent ev);

    public event ProcessEventHandler OnProcess;
    public event ProcessEventHandler OnPhysicsProcess;
    public event InputEventHandler OnInput;
    public event InputEventHandler OnUnhandledInput;

    private static AppHost? _instance = null;
    public static AppHost? Instance
    {
      get { return AppHost._instance; }
    }
    #endregion

    #region Ctor
    public AppHost(Node autoLoadNode)
    {
      this.AutoLoadNode = autoLoadNode;

      AppHost._instance = AppHost.Instance ?? this;
    }
    #endregion

    #region Public Methods
    public void Start(Boolean deferBoot = false)
    {
      GD.Print("[GoDough] Building AppHost");
      var builder = Host.CreateDefaultBuilder(null);

      GD.Print("[GoDough] Booting Dependency Container");
      this.ConfigureBuilder(builder);

      GD.Print("[GoDough] Sealing AppHost");
      this.Application = builder.Build();

      if (!deferBoot)
      {
        this.Boot();
      }
    }

    /// <summary>
    /// Applies the host configuration pipeline: the host-builder hook first, then logging,
    /// then services. Kept separate from <see cref="Start"/> so the pipeline can be verified
    /// without launching the engine.
    /// </summary>
    protected virtual IHostBuilder ConfigureBuilder(IHostBuilder builder)
    {
      return this.ConfigureIHostBuilder(builder)
        .ConfigureLogging(loggingBuilder => this.ConfigureLogging(loggingBuilder))
        .ConfigureServices(services => this.ConfigureServices(services));
    }

    /// <summary>
    /// Configures the host builder before logging and services are applied. Override to replace
    /// the service provider factory (for example Autofac via <c>UseServiceProviderFactory</c>)
    /// or to configure host-level logging such as Serilog. Do not configure logging or services here.
    /// </summary>
    public virtual IHostBuilder ConfigureIHostBuilder(IHostBuilder hostBuilder)
    {
      return hostBuilder;
    }

    /// <summary>
    /// Whether the default engine console logger is registered. Override to return false to remove it.
    /// </summary>
    protected virtual bool AddGodotLoggerByDefault => true;

    /// <summary>
    /// Minimum log level applied when the consumer does not override it. Defaults to
    /// <see cref="LogLevel.Trace"/> in debug builds and to no override otherwise.
    /// </summary>
    protected virtual LogLevel? DefaultMinimumLogLevel =>
      OS.IsDebugBuild() ? LogLevel.Trace : null;

    public virtual void ConfigureLogging(ILoggingBuilder loggingBuilder)
    {
      if (this.AddGodotLoggerByDefault)
      {
        loggingBuilder.AddGodotLogger();
      }

      var minimumLevel = this.DefaultMinimumLogLevel;
      if (minimumLevel.HasValue)
      {
        loggingBuilder.SetMinimumLevel(minimumLevel.Value);
      }
    }

    public virtual void ConfigureServices(IServiceCollection services)
    {
      services
        .AddSingleton<IAppHostNodeProvider, AppHostNodeProvider>(x =>
          new AppHostNodeProvider(() => this.AutoLoadNode))
        .AddSingleton(typeof(SceneManagementService<>));
    }

    public void Boot()
    {
      var logger = this.Application.Services.GetService<ILogger<AppHost>>();

      var bootstrappers = this.Application.Services.GetServices<IBootstrapper>();
      logger.LogInformation("Booting App with {0} Bootstrappers", bootstrappers.Count());

      foreach (var service in bootstrappers)
      {
        service.Run();
      }
    }

    public void PhysicsProcess(double delta)
    {
      if (this.OnPhysicsProcess != null)
      {
        this.OnPhysicsProcess.Invoke(this, delta);
      }

      this.InvokeLifeCycleHooks<IOnPhysicsProcess>(x =>
        x.OnPhysicsProcess(delta));
    }



    public void UnhandledInput(InputEvent ev)
    {
      if (this.OnUnhandledInput != null)
      {
        this.OnUnhandledInput.Invoke(this, ev);
      }

      this.InvokeLifeCycleHooks<IOnUnhandledInput>(x =>
        x.OnUnhandledInput(ev));
    }

    public void Input(InputEvent ev)
    {
      if (this.OnInput != null)
      {
        this.OnInput.Invoke(this, ev);
      }

      this.InvokeLifeCycleHooks<IOnInput>(x =>
        x.OnInput(ev));
    }


    public void Process(double delta)
    {
      if (this.OnProcess != null)
      {
        this.OnProcess.Invoke(this, delta);
      }

      this.InvokeLifeCycleHooks<IOnProcess>(x =>
        x.OnProcess(delta));
    }
    #endregion

    #region Private Methods
    private void InvokeLifeCycleHooks<T>(Action<T> action)
    {
      var framebasedServices = this.Application.Services.GetServices<T>();
      foreach (var service in framebasedServices)
      {
        action(service);
      }
    }
    #endregion
  }
}
