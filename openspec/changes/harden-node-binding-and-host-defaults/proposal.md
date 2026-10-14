# Proposal

## Why

- **节点绑定与服务注入质量不对等**：上一轮把 `[Inject]` 强化为「公有+私有属性与字段、可诊断报错」，节点绑定也跟进到了同等质量，但**只支持唯一命名节点（`%`）**，无法绑定普通子节点，也无法用相对/绝对路径定位。
- **真实场景需要非唯一节点**：大量 UI 结构（容器下的同级控件、`VBox/HBox` 嵌套）并没有唯一命名，仅靠 `%` 无法表达；开发者被迫退回手写 `GetNode(...)`。
- **需要相对与绝对路径**：相对路径（`Hud/Score`）适合场景内部结构，绝对路径（`/root/...`）适合跨子树的稳定引用。
- **名字派生对字段不友好**：C# 私有字段惯例是 `_camelCase`，而 Godot 唯一命名通常是 `PascalCase`；沿用「原样成员名」回退会让 `[UniqueNode] private Label _score` 去找 `%_score`（几乎永远命中不了）。
- **使用者约束**：服务注入与节点注入**保持相互独立的管线**（沿用上一轮结论）。

## What Changes

- **`[UniqueNode]` 保持不变**：仍是唯一命名（`%`）绑定，向后兼容。
- **新增 `[FromNode]`**：把参数作为 Godot **NodePath 表达式原样**解析，覆盖
  - 单段子节点（非唯一）：`"Title"`
  - 相对路径：`"Hud/Score"`
  - 绝对路径：`"/root/Main/Hud/Score"`
  - 唯一命名：`"%Score"`（与 `[UniqueNode("Score")]` 等价）
  - 父级：`".."`
  - 省略路径：回退成员名（去前导 `_`），作为相对路径解析
- **名派生规则统一**：`[UniqueNode]` 与 `[FromNode]` 在未显式指定时，**均由成员名派生并去掉前导下划线**（`_score` → `score`）。
- **共享语义**：成员范围（公有/私有属性与字段、跳过索引器与不可写成员）、`NodeNotFoundException`、`NodeBindingException`、`ignoreInexistent`，以及「与服务注入相互独立」。
- **非目标**
  - 不合并服务注入与节点注入管线。
  - 不做非成员目标的绑定（方法、构造参数、资源/脚本绑定）。
  - 不改动 `GodotApi` 异步加载、`Dispatcher` 并发、日志 provider（见 Follow-ups）。

## Capabilities

### New Capabilities
- `unique-node-binding`：唯一命名节点绑定——变更点在于**名派生规则**（去掉前导 `_`）与明确「路径绑定是另一特性」。
- `node-path-binding`：路径化节点绑定——`[FromNode]` 以 NodePath 表达式（子节点/相对/绝对/唯一/父级）解析目标。
- `host-default-registration`：宿主默认服务注册——开箱可解析框架自带服务并可被覆盖。

### Modified Capabilities
<!-- openspec/specs/ 仍为空（本变更尚未归档），其 delta 尚未落盘，因此不产生跨变更的 MODIFIED -->

## Impact

- **代码**
  - `src/Visuals/Attributes/FromNode.cs`（新增特性）
  - `src/Visuals/Components/NodeBindingPlan.cs`（同时识别 `[UniqueNode]` 与 `[FromNode]`；名派生去前导 `_`；产出 NodePath 表达式）
  - `src/Visuals/Components/NodeExtensions.cs`（统一按表达式 `GetNodeOrNull`，诊断信息带成员与宿主）
  - `src/Runtime/AppHost.cs` 等（默认注册，见 `host-default-registration`）
- **兼容性**：`[UniqueNode]` 的**唯一命名语义不变**；仅「未指定名字时的回退」由「原样成员名」变为「去掉前导 `_`」——对无下划线前缀的成员（属性）行为不变，对 `_` 前缀字段由「几乎必然失败」变为「命中 `camelCase` 命名」。
- **测试**：`NodeBindingTests` 覆盖两种特性的表达式生成、相对/绝对路径原样、`_` 去前缀、派生特性与 `ignoreInexistent`。
- **CI/发布**：无 SDK / TFM 变更。

## Follow-ups（本次不做，另行提案）

- `GodotApi.LoadResourceAsync` 主线程化：改为在既有 Timer 的 `OnProcess` 轮询状态、`Loaded` 时完成 `TaskCompletionSource`，并去掉 `LoadResource<T>` 的阻塞 `.Result`。
- `Dispatcher` 并发修复（`TryRemove(key, out var v)` 且仅在成功时执行）。
- `GoDough.Threading.Dispatcher` 与 `Godot.Dispatcher` 重名 → 建议改名 `MainThreadDispatcher`。
- 质量清理：`<Nullable>enable</Nullable>`（含新特性引入的 `string?` 注解）、`GodotLoggerProvider` 的 `[ProviderAlias("Console")]` 修正、`AppHost` 的 `Dispose`/`Stop`。
- 约定式自动注册（`AddGoDough()` 扫描 `IBootstrapper` / `IOnProcess` / 组件）。
