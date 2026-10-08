using System;
using System.Collections.Generic;
using System.Linq;
using GoDough.Diagnostics.Logging;
using GoDough.Runtime;
using Godot;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GoDough.Tests;

internal sealed class CapturingLoggerProvider : ILoggerProvider
{
    public List<LogLevel> Levels { get; } = new();

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(Levels);

    public void Dispose() { }
}

internal sealed class CapturingLogger : ILogger
{
    private readonly List<LogLevel> _levels;

    public CapturingLogger(List<LogLevel> levels) => _levels = levels;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter) =>
        _levels.Add(logLevel);
}

internal sealed class LoggingProbeAppHost : AppHost
{
    private readonly bool _addGodotLogger;
    private readonly LogLevel? _minimumLevel;

    public LoggingProbeAppHost(bool addGodotLogger, LogLevel? minimumLevel) : base(null!)
    {
        _addGodotLogger = addGodotLogger;
        _minimumLevel = minimumLevel;
    }

    protected override bool AddGodotLoggerByDefault => _addGodotLogger;

    protected override LogLevel? DefaultMinimumLogLevel => _minimumLevel;
}

public class LoggingTests
{
    [Fact]
    public void AddGodotLogger_registers_godot_provider_by_default()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddGodotLogger());

        using var provider = services.BuildServiceProvider();

        Assert.Contains(provider.GetServices<ILoggerProvider>(), p => p is GodotLoggerProvider);
    }

    [Fact]
    public void Additional_logger_provider_is_composed_with_the_default_one()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder =>
        {
            builder.AddGodotLogger();
            builder.AddProvider(new CapturingLoggerProvider());
        });

        using var provider = services.BuildServiceProvider();
        var providers = provider.GetServices<ILoggerProvider>().ToList();

        Assert.Contains(providers, p => p is GodotLoggerProvider);
        Assert.Contains(providers, p => p is CapturingLoggerProvider);
    }

    [Fact]
    public void Log_level_filter_from_configuration_is_applied()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Logging:LogLevel:Default"] = "Warning",
            })
            .Build();

        var capture = new CapturingLoggerProvider();
        using var factory = LoggerFactory.Create(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddProvider(capture);
        });

        var logger = factory.CreateLogger("Test");
        logger.LogInformation("info");
        logger.LogWarning("warn");

        Assert.DoesNotContain(LogLevel.Information, capture.Levels);
        Assert.Contains(LogLevel.Warning, capture.Levels);
    }

    [Fact]
    public void Default_engine_logger_can_be_removed_and_kept()
    {
        var enabled = new ServiceCollection();
        enabled.AddLogging(b => new LoggingProbeAppHost(addGodotLogger: true, minimumLevel: null).ConfigureLogging(b));

        var disabled = new ServiceCollection();
        disabled.AddLogging(b => new LoggingProbeAppHost(addGodotLogger: false, minimumLevel: null).ConfigureLogging(b));

        using var enabledProvider = enabled.BuildServiceProvider();
        using var disabledProvider = disabled.BuildServiceProvider();

        Assert.Contains(enabledProvider.GetServices<ILoggerProvider>(), p => p is GodotLoggerProvider);
        Assert.DoesNotContain(disabledProvider.GetServices<ILoggerProvider>(), p => p is GodotLoggerProvider);
    }

    [Fact]
    public void Configurable_default_minimum_level_is_applied()
    {
        var services = new ServiceCollection();
        var capture = new CapturingLoggerProvider();
        services.AddLogging(b =>
        {
            new LoggingProbeAppHost(addGodotLogger: false, minimumLevel: LogLevel.Warning).ConfigureLogging(b);
            b.AddProvider(capture);
        });

        using var provider = services.BuildServiceProvider();
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("Test");
        logger.LogInformation("info");
        logger.LogWarning("warn");

        Assert.DoesNotContain(LogLevel.Information, capture.Levels);
        Assert.Contains(LogLevel.Warning, capture.Levels);
    }
}
