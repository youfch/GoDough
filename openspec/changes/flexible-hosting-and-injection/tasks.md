# Tasks

## 1. 平台基线升级

- [x] 1.1 更新 `src/GoDough.csproj`：`Godot.NET.Sdk` 版本对齐 Godot 4.7.2、`TargetFramework` 改为 `net8.0`、`Microsoft.Extensions.*` 升到 `8.0.x`，保留 `EnableDynamicLoading`；验证 `dotnet build src/GoDough.sln -c Release` 成功
- [x] 1.2 确认发布产物路径仍为 `.godot/mono/temp/bin/Release/GoDough.<version>.nupkg`，必要时同步 `.github/release-package.sh`；验证 `dotnet pack` 产物路径与脚本引用一致

## 2. 宿主扩展点

- [x] 2.1 在 `AppHost` 新增 `virtual IHostBuilder ConfigureIHostBuilder(IHostBuilder)`，并在 `Start()` 中于 `ConfigureLogging` / `ConfigureServices` 之前调用；验证子类覆写的方法在启动时被调用到
- [x] 2.2 确认默认调用链与默认容器行为不变；验证未覆写钩子时现有启动流程照常工作

## 3. 日志可插拔

- [x] 3.1 调整 `ConfigureLogging`：默认注册引擎控制台 sink 且可被覆写/移除；验证默认启动控制台可见日志，覆写且不调 base 时不写控制台但仍能启动
- [x] 3.2 将调试构建强制 `LogLevel.Trace` 改为可覆盖的默认值；验证调试构建默认详细、使用者覆盖后以其设置为准
- [x] 3.3 支持追加第三方 `ILoggerProvider`（NLog / log4net 作为文档示例）；验证注册后能与默认 sink 共存接收日志
- [x] 3.4 确认配置源过滤（`Logging:LogLevel`）生效；验证按分类设置级别后过滤结果正确

## 4. 依赖注入容器可替换

- [x] 4.1 通过 `ConfigureIHostBuilder` 支持 `UseServiceProviderFactory(...)`；验证以 Autofac 工厂启动时可解析应用服务
- [x] 4.2 确保框架内部对自身服务的解析仅经由 `IServiceProvider`；验证自定义容器下逐帧/输入生命周期钩子与引导器仍按既有节奏触发
- [x] 4.3 验证 `MapSingleton` 多态映射与 `AddFactory` 工厂注册在自定义容器下仍可解析

## 5. `[Inject]` 强化

- [x] 5.1 扩展 `InjectAttribute`：新增 `Key` 与 `Required` 参数，并将 `AttributeUsage` 扩展到属性与字段；验证项目编译通过
- [x] 5.2 修正特性匹配为 `typeof(InjectAttribute).IsAssignableFrom(attr.GetType())`；验证使用派生特性的成员可被注入
- [x] 5.3 支持公有与私有属性以及字段的注入；验证私有属性与字段注入成功
- [x] 5.4 实现必填/可选与键控解析（`GetRequiredService` / `GetRequiredKeyedService`）；验证必填缺失抛可诊断错误、可选缺失跳过、按键注入命中正确实例
- [x] 5.5 处理无 getter 的属性等边界；验证此类成员不导致注入抛出异常

## 6. 测试与 CI

- [x] 6.1 建立最小测试工程（默认 xUnit），覆盖 `[Inject]` 语义与默认日志行为；验证 `dotnet test` 通过
- [x] 6.2 修正 CI 中的 `dotnet test` 指向该测试工程；验证工作流构建与测试步骤通过

## 7. 文档与发布

- [x] 7.1 更新 README / 文档：补充 NLog、log4net、Autofac 接入示例与 `ConfigureIHostBuilder` 用法；验证文档含可运行示例
- [x] 7.2 在发布说明中注明 `[Inject]` 行为修复与 net8 / Godot 4.7.2 升级；验证 release notes 已包含该说明