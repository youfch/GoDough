# Design

## Context

- 当前 `src/Visuals/Components/NodeExtensions.cs` 的 `BindToViewModel` 只对 `GetProperties()`（公有属性）做 `[UniqueNode]` 扫描，通过 `node.GetNode("%{name}")` 取唯一命名节点并 `SetValue`；失败时抛 `NodeNotFoundException`，类型不符时抛出的是反射原生 `ArgumentException`。
- 上一变更 `flexible-hosting-and-injection` 强化了 `[Inject]`（公有+私有、属性+字段、可诊断报错），并把节点绑定明确列为非目标，造成两者能力不对称。
- `src/Runtime/AppHost.cs` 的 `ConfigureServices` 默认只注册 `IAppHostNodeProvider` 与 `SceneManagementService<>`；而 `SceneManagementService` 的构造函数依赖 `IGodotApi`、`Dispatcher`、`IAppHostNodeProvider`、`ILogger<>`，其中 `IGodotApi` 与 `Dispatcher` 未默认注册；`GodotApi` 与 `ComponentFactory` 又依赖 `AppHost` 本身（亦未默认注册）。
- 约束（使用者明确）：服务注入与节点注入**相互独立的管线**；节点绑定**只处理唯一命名节点**，目标由 `SpecificName`（回退成员名）决定。

## Goals / Non-Goals

**Goals:**
- 让节点绑定与服务注入在「成员范围」与「失败诊断」上质量对等。
- 保持两条管线在语义与调用上相互独立。
- 让框架自带服务开箱即可解析，消除「默认注册了却解析不了」的隐患。
- 默认注册可被使用者覆盖。

**Non-Goals:**
- 不合并服务注入与节点注入管线。
- 不引入 `NodePath` / 任意路径查找，不新增节点绑定特性。
- 不改动 `GodotApi` 异步加载、`Dispatcher` 并发、日志 provider、`AppHost` 释放（见 proposal 的 Follow-ups）。
- 不强化 `Component` / `ComponentFactory` 的既有装配语义。

## Decisions

### D1. 服务注入与节点注入保持两条独立管线
`WireNode()`（`[Inject]`，容器解析）与节点绑定（`[UniqueNode]`，场景树查找）保持为互不调用的独立入口，使用者按需分别调用。
- 理由：两者语义不同（容器解析 vs 场景树查找），失败原因与诊断维度不同；合并会迫使所有节点承担不必要的开销，也会掩盖各自的失败语义。使用者已明确要求分开。
- 备选：单一 `WireNode()` 一次完成两者——被使用者否决，排除。

### D2. 节点绑定沿用「唯一命名节点」，目标名由 `SpecificName`（回退成员名）决定
绑定目标 SHALL 为 `%{UniqueNode.SpecificName ?? 成员名}`，不引入 `NodePath` / 路径查找。
- 理由：唯一命名（`%`）是 Godot 官方推荐的场景内引用方式，稳定且不随层级变动；路径查找脆弱、易随场景结构调整而失效。使用者已明确。
- 备选：支持任意 `NodePath`——被否决，排除。

### D3. 节点绑定成员范围对齐 `[Inject]`
扫描 `BindingFlags.Instance | Public | NonPublic`，属性与字段一并处理，跳过索引器与不可写成员（get-only 属性、readonly/const 字段）。
- 理由：与 `[Inject]` 的成员契约保持一致，降低认知成本；「跑得起来但不生效」的隐性坑（私有成员被静默忽略）比显式失败更危险。
- 备选：仅公有属性（现状）——不满足对等目标，排除。

### D4. 绑定失败给出可诊断错误
- 成员类型与目标节点类型不符：抛出包含「成员名 + 期望类型 + 实际类型」的错误。
- 目标节点缺失：默认抛 `NodeNotFoundException`（含节点名）；`IgnoreInexistent = true` 时跳过。
- 理由：与 `[Inject]` 的失败语义对齐；反射原生异常的「Object of type X cannot be converted to type Y」不含成员上下文，排查成本高。
- 备选：静默置空——与整体「显式失败」取向冲突，排除。

### D5. 默认注册补全采用 `TryAdd*` 语义
`AppHost.ConfigureServices` 以 `TryAdd*` 注册 `AppHost`、`IGodotApi`、`Dispatcher`（含 `IOnProcess` 映射）、`ComponentFactory`。
- 理由：既保证开箱可用，又不妨碍使用者覆盖；`TryAdd` 不覆盖已存在的注册，符合「默认值可被替换」的心智。
- 备选：直接 `AddSingleton`——会产生重复注册（多描述符、后者生效），语义不如 `TryAdd` 明确，排除。
- 兼容性提示：`AddGodotRuntimeUtilities` / `AddThreadingUtilities` 内部目前使用 `AddSingleton`，与默认注册可能并存；复核后如需要，一并改为 `TryAdd*` 以保持幂等。

## Risks / Trade-offs

- [默认注册变更影响既有使用者的注册顺序/覆盖] → 以 `TryAdd*` 保证不覆盖使用者注册；在 README 与 release notes 说明新增的默认注册项。
- [私有成员绑定的反射开销] → 仅在调用 `BindToViewModel` 时执行一次，量级可忽略；与 `[Inject]` 现状一致。
- [成员范围扩大后，此前「恰好能工作」的用法可能暴露新错误（如私有成员同名节点缺失触发抛错）] → 视为修复；通过 `IgnoreInexistent` 提供豁免，并在文档中说明。
- [节点绑定在无引擎环境下难以单元测试] → 优先覆盖纯逻辑（成员筛选、目标名解析、异常消息）；确需引擎的部分以最小场景在样例中验证。

## Migration Plan

1. 重写 `BindToViewModel`（成员范围 + 目标名解析 + 诊断），保持签名与命名空间不变，避免破坏调用点。
2. 在 `AppHost.ConfigureServices` 补默认注册（`TryAdd*`），复核与既有 `AddXxxUtilities` 的共存。
3. 补充单元测试与 README 说明。
4. 回归 `samples/GoDough.Demo`：移除为绕过默认注册而手写的注册，确认行为不变。
5. 发布为次版本；在 release notes 注明默认注册新增项与节点绑定的诊断变化。
- 回滚：改动以「扩大范围 + 新增注册」为主，回退不涉及数据迁移。

## Open Questions

- 节点绑定的诊断错误：复用 `NodeNotFoundException` 还是新增专用异常（如 `NodeBindingException`）？倾向新增专用异常以区分「查找失败」与「类型不符」两类。
- 是否需要为 `[UniqueNode]` 补 `Required` 参数（与 `[Inject(Required)]` 对齐），还是沿用 `IgnoreInexistent`？倾向沿用现有参数以免引入两套语义。
- 默认注册补全是否同步把 `AddGodotRuntimeUtilities` / `AddThreadingUtilities` / `AddComponentFactory` 改为 `TryAdd*` 并标记为「可选、幂等」？
