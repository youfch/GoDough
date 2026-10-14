using GoDough.Runtime.LivecycleHooks;

using Microsoft.Extensions.Logging;

namespace GoDough.Demo;

/// <summary>
/// 生命周期钩子：宿主每帧调用 OnProcess（由 AppHostLoader._Process → AppHost.Process 驱动）。
/// 注意命名空间拼写是 LivecycleHooks。
/// </summary>
public sealed class Counter : IOnProcess
{
    private readonly ILogger<Counter> _logger;

    public Counter(ILogger<Counter> logger) => _logger = logger;

    public int Frames { get; private set; }

    public void OnProcess(double delta)
    {
        Frames++;

        // 每 180 帧打一条，避免刷屏。
        if (Frames % 180 == 0)
        {
            _logger.LogInformation("Counter: 已运行 {Frames} 帧", Frames);
        }
    }
}