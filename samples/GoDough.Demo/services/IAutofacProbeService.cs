namespace GoDough.Demo;

/// <summary>用 Autofac 原生 API 注册的服务（见 GameAppHost.ConfigureIHostBuilder）。</summary>
public interface IAutofacProbeService
{
    string Describe();
}

public sealed class AutofacProbeService : IAutofacProbeService
{
    public string Describe() => "由 Autofac ContainerBuilder 原生注册并解析";
}