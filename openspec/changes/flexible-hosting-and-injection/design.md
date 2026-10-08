# Design

## Context

- 当前宿主在 `AppHost.Start()` 中直接 `Host.CreateDefaultBuilder(null)` 就地配置日志与服务，`ConfigureLogging(ILoggingBuilder)` 已可覆写，但**没有**在容器构建前暴露 `IHostBuilder` 的扩展点，因此无法替换 `IServiceProviderFactory`（见 proposal.md - Why）。
- 节点注入在 `Composition/Extensions/NodeExtension.cs` 中用反射扫描 `[Inject]`：匹配判断方向写反、只扫公有属性、解析失败静默置空。
- 约束：必须继续使用 `Godot.NET.Sdk`（非目标里已锁定），继续保持核心库对第三方日志/容器的可选性；框架内部所有服务解析只允许经由 `IServiceProvider`。
- 参考：移植分支已出现 `ConfigureIHostBuilder(IHostBuilder)` + `Pipe(...)` 的形态，可回收其思路，但不沿用其 `GDemo.Host` 命名空间。

## Goals / Non-Goals

**Goals:**
- 在容器/日志构建之前提供一个稳定的宿主级扩展点。
- 让日志既能「追加提供程序」也能「整体替换宿主日志器」，且默认行为零配置可用。
- 让依赖注入容器可由使用者替换，框架自身保持容器无关。
- 让 `[Inject]` 的匹配、可见性、键控、必填语义可预测且可诊断。

**Non-Goals:**
- 不切换到 `Microsoft.NET.Sdk`，不显式引 `GodotSharp` 包。
- 不强化 `[UniqueNode]` / 视图绑定。
- 不在核心库内强引 NLog / log4net / Autofac；不在本次引入源生成器或自研容器抽象。
- 不改动场景管理与组件工厂的既有行为。

## Decisions

### D1. 宿主扩展点：新增 `virtual IHostBuilder ConfigureIHostBuilder(IHostBuilder)`
在 `Start()` 中于 `ConfigureLogging` / `ConfigureServices` **之前**调用，等价于把移植分支的钩子收回并去掉 `Pipe` 的额外命名空间（`Pipe` 可直接内联为局部调用）。
- 理由：换容器、Serilog 等必须在 `IHostBuilder` 层介入，`IServiceCollection` 层做不到。
- 备选：只提供 `ConfigureServices` —— 无法替换 `IServiceProviderFactory`，排除。
- 备选：构造期传入委托配置 —— 增加 API 面积且不利于继承覆写，排除。

### D2. 日志分层：provider 型 vs 宿主替换型
- provider 型（NLog / log4net / 引擎控制台）统一走 `ConfigureLogging(ILoggingBuilder)`：`AddGodotLogger()` 作为默认 sink，使用者可保留或移除并追加第三方 provider。
- 宿主替换型（Serilog）走 D1 的 `ConfigureIHostBuilder`。
- 把「调试构建强制 `LogLevel.Trace`」改为可覆盖默认值，避免压制第三方配置。
- 理由：与 ASP.NET 的 `builder.Logging.AddXxx()` / `builder.Host.UseSerilog()` 心智一致；核心库保持零强依赖。
- 备选：自研 `ILogSink` 抽象 —— 与 `ILoggerProvider` 重复，排除。
- 备选：核心包直接依赖 NLog —— 违背轻量目标，排除。

### D3. DI 替换：依赖 `UseServiceProviderFactory`
通过 D1 的钩子接收 `AutofacServiceProviderFactory` 之类的工厂；框架内部及节点注入统一用 `IServiceProvider.GetService/GetServices`，不使用任何具体容器 API。
- 理由：`Microsoft.Extensions.Hosting` 已标准化该扩展点，无需自建抽象。
- 备选：定义自有 `IServiceContainer` 接口 —— 徒增适配层，排除。

### D4. `[Inject]` 重写为统一的解析器
- 属性与字段一并扫描：`BindingFlags.Instance | Public | NonPublic`。
- 特性匹配改为 `typeof(InjectAttribute).IsAssignableFrom(attr.GetType())`，使派生特性生效。
- 解析默认必填：优先 `GetRequiredService`；可选时跳过；键控用 `GetRequiredKeyedService`。
- `InjectAttribute` 增加 `Key`（默认 `null`）与 `Required`（默认 `true`）参数，并将 `AttributeUsage` 扩展到属性与字段。
- 跳过索引器与无 setter/无字段可写的情形，保证不因成员形态抛异常。
- 理由：一次性覆盖移植分支 `WireAllNode` 的价值并修复既有 bug；语义显式、错误可诊断。
- 备选：源生成器（编译期注入）—— 性能更好但成本高，列为后续可选，排除于本次。
- 备选：仅补齐移植分支的 `WireAllNode`（公私属性）—— 不修 bug、不含键控/可选，排除。

### D5. `[UniqueNode]` 不动
需求 4 明确排除，`BindToViewModel` 与相关特性保持现状。

## Risks / Trade-offs

- [覆写 `ConfigureLogging` 时忘记调用默认逻辑导致引擎日志消失] → 在文档与 design 中说明 base 调用语义；默认路径零配置仍输出控制台。
- [`WireNode` 匹配 bug 修复会改变派生特性的行为（此前不生效、现在生效）] → 视为修复并写入 release notes；对精确 `[Inject]` 用法无影响。
- [第三方容器下 `GetServices<T>` 的解析差异] → 以 `IServiceProvider` 通用能力为准，specs 中以「自定义容器下生命周期钩子/注入仍可用」的场景约束实现，必要时加集成验证。
- [net8 键控服务要求使用者也用 net8 容器] → 键控为可选特性，非键控用法在默认容器下不受影响。
- [CI `dotnet test` 无测试工程] → 本次一并建最小测试工程或移除该步骤，避免发布链空跑失败。

## Migration Plan

1. 升级 `src/GoDough.csproj`（SDK 版本对齐 Godot 4.7.2、TFM `net8.0`、M.E. `8.0.x`），确认 `EnableDynamicLoading` 与发布路径不变。
2. 引入 D1 钩子并调整 `Start()` 调用链，保持默认行为不变。
3. 调整日志默认行为（D2），确保零配置仍输出控制台。
4. 重写 `[Inject]` 解析（D4）并修 bug。
5. 视需要补充测试；修正 CI 的 `dotnet test`。
6. 发布为次版本；`WireNode` 行为变化在 release notes 注明。
- 回滚：改动以新增 API 为主，回退到上一版本不涉及数据迁移。

## Open Questions

- 是否在后续版本随库提供 `GoDough.Logging.NLog` / `GoDough.Composition.Autofac` 可选集成包（本次先以钩子 + 文档覆盖）。
- 测试工程的框架选择（xUnit/NUnit）与是否需要面向引擎的测试宿主，可在实施阶段确定。
