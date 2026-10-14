using Microsoft.Extensions.Logging;

namespace GoDough.Demo;

/// <summary>默认问候服务（未键控）。</summary>
public sealed class Greeter : IGreeter
{
    private readonly ILogger<Greeter> _logger;

    public Greeter(ILogger<Greeter> logger) => _logger = logger;

    public string Greet(string name)
    {
        _logger.LogInformation("[Greeter] Hello {Name}", name);
        return $"Hello, {name}!";
    }
}