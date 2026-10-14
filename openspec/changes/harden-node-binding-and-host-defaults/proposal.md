# Proposal

## Why

- **节点绑定与服务注入质量不对等**：上一变更 `flexible-hosting-and-injection` 把 `[Inject]` 强化为「公有+私有属性与字段、可诊断报错」，但**节点绑定**（`[UniqueNode]` + `BindToViewModel`）被明确列为非目标，至今仍只扫描**公有属性**，且失败时抛的是反射原生异常（不指出成员、期望类型与实际类型），排查成本高。
- **宿主默认注册不完整**：`AppHost.ConfigureServices` 默认注册了 `SceneManagementService<>`，但它依赖的 `IGodotApi`、`Dispatcher`（以及 `AppHost` 自身、`ComponentFactory`）**都不在默认注册内**——使用者必须额外调用 `AddGodotRuntimeUtilities()` / `AddThreadingUtilities()` / `AddSingleton<AppHost>(this)`，漏掉任意一项就在运行时抛解析异常。等价于「框架默认注册了一个自己解析不了的服务」。
- **使用者的明确约束**：服务注入与节点注入**保持相互独立的管线**；节点绑定**只处理唯一命名节点（`%`）**，目标由 `UniqueNode.SpecificName`（为空则回退成员名）决定，**不引入路径查找**。

## What Changes

- **节点绑定强化（保持独立管线）**
  - `BindToViewModel` 成员扫描扩展到 `BindingFlags.Instance | Public | NonPublic`，**属性与字段一并处理**，跳过索引器与不可写成员。
  - 保持 `%唯一命名` 语义：目标名 = `UniqueNode.SpecificName ?? 成员名`；**不引入** `NodePath` / 任意路径查找。
  - 缺失或类型不符时抛出**可诊断**错误（成员名 + 期望类型 + 实际类型 / 节点名），复用或扩展 `NodeNotFoundException`。
  - 与服务注入**互不调用**，两条管线可各自单独使用。
- **宿主默认注册补全**
  - `AppHost.ConfigureServices` 默认注册完整依赖图：`AppHost` 自身、`IGodotApi`、`Dispatcher`（含 `IOnProcess` 映射）、`ComponentFactory`，使 `SceneManagementService<>` **开箱即可解析**。
  - 采用 `TryAdd*` 语义，保证使用者仍可覆盖默认实现。
- **非目标**
  - 不合并服务注入与节点注入管线。
  - 不为节点绑定引入 `NodePath`/路径查找、不新增节点特性。
  - 不改动 `GodotApi` 的异步加载实现、`Dispatcher` 并发、日志 provider（见 Follow-ups）。

## Capabilities

### New Capabilities
- `unique-node-binding`：节点绑定契约——唯一命名节点解析、可绑定成员范围、失败诊断，且与服务注入相互独立。
- `host-default-registration`：宿主默认服务注册契约——开箱可解析框架自带服务（场景管理及其依赖），并可被使用者覆盖。

### Modified Capabilities
<!-- openspec/specs/ 仍为空（上一变更尚未归档），本次不修改既有 capability -->

## Impact

- **代码**
  - `src/Visuals/Components/NodeExtensions.cs`（`BindToViewModel` 重写）
  - `src/Visuals/Attributes/UniqueNode.cs`（如需补 `Required` 语义）
  - `src/Runtime/AppHost.cs`（`ConfigureServices` 默认注册补全）
  - `src/Visuals/Components/ComponentFactory.cs`（节点绑定的既有调用点，行为一致）
- **兼容性**：`BindToViewModel` 由「仅公有属性」扩为「公有+私有、属性+字段」属**增强/修复**，此前可用的用法不受影响；默认注册为**新增注册项**，与既有使用方式兼容（`TryAdd*` 允许覆盖）。
- **测试**：在既有 xUnit 工程补节点绑定与默认注册的单元测试（注意：节点绑定需 `Node` 实例，纯逻辑部分优先；引擎相关部分以最小场景验证）。
- **CI/发布**：无 SDK / TFM 变更；提交类型据范围选择（本轮为新增能力，倾向 `feat`）。

## Follow-ups（本次不做，另行提案）

- `GodotApi.LoadResourceAsync` 主线程化：当前在后台线程 `LoadThreadedRequest` 后立即 `LoadThreadedGet`，`InProgress` 时可能取到 `null`；应改为在既有 Timer 的 `OnProcess` 轮询状态、`Loaded` 时完成 `TaskCompletionSource`，并去掉 `LoadResource<T>` 的阻塞 `.Result`。
- `Dispatcher` 并发修复：`while(Count>0){ FirstOrDefault(); TryRemove(action); …invoke }` 在并发下可能移除失败却仍执行；改为 `TryRemove(key, out var v)` 且仅当返回 `true` 才执行。
- 质量清理：`<Nullable>enable</Nullable>`、`GodotLoggerProvider` 的 `[ProviderAlias("ColorConsole")]` 修正、`GodotApi` 未使用字段、`AppHost` 的 `Dispose`/`Stop`。
- 约定式自动注册：`AddGoDough()` 扫描 `IBootstrapper` / `IOnProcess` / `Component`。
