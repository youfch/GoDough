using GoDough.Runtime.LivecycleHooks;

using Microsoft.Extensions.Logging;

namespace GoDough.Demo;

/// <summary>
/// Lifecycle hook: the host calls OnProcess every frame (driven by
/// AppHostLoader._Process -&gt; AppHost.Process). Note the namespace is spelled
/// "LivecycleHooks".
/// </summary>
public sealed class Counter : IOnProcess
{
    private readonly ILogger<Counter> _logger;

    public Counter(ILogger<Counter> logger) => _logger = logger;

    public int Frames { get; private set; }

    public void OnProcess(double delta)
    {
        Frames++;

        // Log once every 180 frames to avoid spamming the output.
        if (Frames % 180 == 0)
        {
            _logger.LogInformation("Counter: {Frames} frames elapsed", Frames);
        }
    }
}