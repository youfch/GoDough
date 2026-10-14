using Microsoft.Extensions.Logging;

namespace GoDough.Demo;

/// <summary>
/// Keyed greeting service: registered under the "pirate" key and resolved with
/// [Inject("pirate")].
/// </summary>
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