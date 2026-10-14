namespace GoDough.Demo;

/// <summary>
/// Intentionally left unregistered, to demonstrate optional injection:
/// <c>[Inject(Required = false)]</c> is skipped instead of throwing when the service is missing.
/// </summary>
public interface IOptionalService
{
    string Value { get; }
}