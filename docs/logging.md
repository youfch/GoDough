# Logging

- [Logging from a node](#logging-from-a-node)
- [Default sink and level](#default-sink-and-level)
- [Adding more providers (NLog, log4net)](#adding-more-providers-nlog-log4net)
- [Removing the default engine sink](#removing-the-default-engine-sink)
- [Replacing the host logger (Serilog)](#replacing-the-host-logger-serilog)
- [Filtering by configuration](#filtering-by-configuration)

The host wires `Microsoft.Extensions.Logging`. Anything that can consume `ILogger<T>` works, so the
usual .NET logging stack applies.

## Logging from a node

Inject `ILogger<T>` and log:

```cs
using Godot;
using GoDough.Composition.Attributes;
using GoDough.Composition.Extensions;
using Microsoft.Extensions.Logging;

public partial class MyNode : Node
{
    [Inject] private ILogger<MyNode> _logger = null!;

    public override void _Ready()
    {
        this.WireNode();
        _logger.LogDebug("Node is ready.");
    }
}
```

The default engine console sink prints, for example:

```
[2026/10/15 09:14:34] [Debug] [MyNode] Node is ready.
```

Information, Debug, Trace and Warning go to `GD.Print`; Error and Critical go to `GD.PrintErr`.

## Default sink and level

- The **engine console sink** (`AddGodotLogger`) is registered by default.
- The **minimum level** defaults to `LogLevel.Trace` in debug builds (`OS.IsDebugBuild()`) and is left
  to the .NET defaults otherwise.

Both are overridable:

```cs
public sealed class GameAppHost : AppHost
{
    // Remove the default engine console sink.
    protected override bool AddGodotLoggerByDefault => false;

    // Override the default minimum level (null = do not set one).
    protected override LogLevel? DefaultMinimumLogLevel => LogLevel.Information;
}
```

`ConfigureLogging` builds a standard `ILoggingBuilder`, so it composes exactly like ASP.NET Core:

```cs
using Microsoft.Extensions.Logging;

public override void ConfigureLogging(ILoggingBuilder loggingBuilder)
{
    base.ConfigureLogging(loggingBuilder);   // keep the default engine console sink

    loggingBuilder.SetMinimumLevel(LogLevel.Information);
}
```

## Adding more providers (NLog, log4net)

Third-party providers are **added next to** the default sink; the core library has no dependency on
them. Add the provider package to your game project and compose it:

```cs
// NLog.Extensions.Logging
public override void ConfigureLogging(ILoggingBuilder loggingBuilder)
{
    base.ConfigureLogging(loggingBuilder);
    loggingBuilder.AddNLog();
}

// Microsoft.Extensions.Logging.Log4Net.AspNetCore
public override void ConfigureLogging(ILoggingBuilder loggingBuilder)
{
    base.ConfigureLogging(loggingBuilder);
    loggingBuilder.AddLog4Net("log4net.config");
}
```

> NLog looks for `nlog.config` relative to the app base directory, not the project root. When running
> from the editor that is the assembly output directory; after export put `nlog.config` next to the
> executable, or load it explicitly with `LogManager.Setup().LoadConfigurationFromFile(path)`.

## Removing the default engine sink

Return `false` from `AddGodotLoggerByDefault` (see above). The host still starts and logs through
whatever providers you keep.

## Replacing the host logger (Serilog)

Host-level loggers replace the whole `ILoggerFactory` and are wired through the host builder hook:

```cs
public override IHostBuilder ConfigureIHostBuilder(IHostBuilder hostBuilder) =>
    hostBuilder.UseSerilog((context, config) => config.WriteTo.File("log.txt"));
```

## Filtering by configuration

Because the host is a normal .NET host, configuration-based filtering applies — for example
`Logging:LogLevel` in `appsettings.json` or environment variables:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "GoDough.Demo.Greeter": "Information"
    }
  }
}
```