namespace GoDough.Demo;

/// <summary>
/// Service registered through the Autofac-native API (see GameAppHost.ConfigureIHostBuilder).
/// </summary>
public interface IAutofacProbeService
{
    string Describe();
}

public sealed class AutofacProbeService : IAutofacProbeService
{
    public string Describe() => "resolved from a native Autofac ContainerBuilder registration";
}