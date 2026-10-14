using Microsoft.Extensions.Logging;

namespace GoDough.Demo;

/// <summary>键控问候服务：注册键为 "pirate"，用 [Inject("pirate")] 获取。</summary>
public sealed class PirateGreeter : IGreeter
{
    private readonly ILogger<PirateGreeter> _logger;

    public PirateGreeter(ILogger<PirateGreeter> logger) => _logger = logger;

    public string Greet(string name)
    {
        _logger.LogInformation("[PirateGreeter] Yarr {Name}", name);
        return $"Yarr, {name}!";
    }
}