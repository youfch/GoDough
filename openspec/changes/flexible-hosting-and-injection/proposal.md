# Proposal

## Why

GoDough 把日志 provider、DI 容器和节点注入都写死在 `AppHost` 自身实现里：使用者想换成 NLog / log4net 或把容器换成 Autofac，只能 fork 或绕过框架；同时 `[Inject]` 存在特性匹配方向错误、只扫公有属性、失败静默等缺陷。此外项目仍停留在 `net6.0` / `Godot.NET.Sdk 4.0.2`，与当前的 Godot 4.7.2 生态基线脱节。本次变更让宿主从「固定实现」变成「可替换、可组合的宿主」。

## What Changes

- **平台基线**：`src/GoDough.csproj` 的 `Godot.NET.Sdk` 升级到与 Godot 4.7.2 对应的版本，`TargetFramework` 升到 `net8.0`，`Microsoft.Extensions.*` 升到 `8.0.x`。
  - **明确非目标**：不切换到 `Microsoft.NET.Sdk`，继续使用 Godot 官方 SDK，因此 `using Godot;`、`GD.Print`、CI 产物路径均不变。
- **宿主扩展点**：`AppHost` 新增 `virtual IHostBuilder ConfigureIHostBuilder(IHostBuilder)`，在 `ConfigureLogging` / `ConfigureServices` 之前暴露 `IHostBuilder`，供替换容器、Serilog 等宿主级配置使用（从移植分支回收该能力，但不沿用其 `GDemo.Host` 命名空间）。
- **日志可插拔**：`ConfigureLogging(ILoggingBuilder)` 保持可组合；`AddGodotLogger` 作为默认 sink，可被覆盖/移除；NLog / log4net 等 provider 型日志通过 `ILoggingBuilder` 组合。核心库**不**硬引用第三方日志包。
- **DI 容器可替换**：通过 `ConfigureIHostBuilder` + `UseServiceProviderFactory(...)` 支持 Autofac 等容器；现有 `GetService/GetServices`、`MapSingleton`、`AddFactory`、`NodeExtension` 保持容器无关，不做改动。
- **[Inject] 强化**：修正特性匹配方向 bug；支持公有+私有属性与字段；缺失服务时用 `GetRequiredService` 显式报错；支持 net8 键控服务 `[Inject("key")]`;支持可选注入 `[Inject(Required = false)]`。
- **[UniqueNode]**：明确**不**强化（使用频率低）。

## Capabilities

### New Capabilities
- `logging-abstraction`: 宿主日志的可组合与可替换行为——默认 Godot sink、第三方 provider 接入、日志级别与过滤的配置方式。
- `service-container-replacement`: 宿主依赖注入容器可替换为第三方容器（如 Autofac）的行为，以及框架内部服务解析对任意 `IServiceProvider` 的兼容约定。
- `node-injection`: `[Inject]` 特性在 Godot 节点上的服务注入语义——匹配规则、可见性覆盖、键控解析、必填/可选与失败行为。

### Modified Capabilities
<!-- 项目当前 openspec/specs/ 为空，无既有 capability 被修改 -->

## Impact

- **代码**：
  - `src/GoDough.csproj`（SDK 版本、TFM、包版本）
  - `src/Runtime/AppHost.cs`（新增 `ConfigureIHostBuilder` 钩子、`Start()` 调用链、日志默认行为）
  - `src/Composition/Extensions/NodeExtension.cs`（`[Inject]` 解析重写）
  - `src/Composition/Attributes/InjectAttribute.cs`（新增可选参数：键、Required）
  - 可能新增 `Runtime/Extensions/HostBuilderExtensions.cs`（`Pipe` 之类的宿主构建扩展）
- **依赖**：`Microsoft.Extensions.Hosting` / `Logging.Abstractions` 升到 8.0.x；不新增对 NLog / log4net / Autofac 的强依赖。
- **兼容性**：`AppHost` 的公开 API 以新增虚方法/新增可选参数为主，现有使用方式保持可用；`WireNode<T>` 行为修正后，此前「恰好能工作」的精确 `InjectAttribute` 用法不受影响，派生特性由「不生效」变为「生效」（属修复）。
- **CI/发布**：SDK 未切换，`.github/release-package.sh` 的产物路径不变；但 CI 中 `dotnet test` 仍无测试工程，本次一并处理（建最小测试工程或移除该步骤）。