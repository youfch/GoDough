namespace GoDough.Demo;

/// <summary>
/// 故意「不注册」的服务，用于演示可选注入：<c>[Inject(Required = false)]</c> 缺失时会被跳过而非报错。
/// </summary>
public interface IOptionalService
{
    string Value { get; }
}