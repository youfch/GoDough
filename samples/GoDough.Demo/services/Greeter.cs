using Microsoft.Extensions.Logging;

namespace GoDough.Demo;

/// <summary>Default (unkeyed) greeting service.</summary>
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