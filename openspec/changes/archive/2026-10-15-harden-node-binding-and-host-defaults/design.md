# Design

## Context

- `src/Visuals/Components/NodeExtensions.cs` 的 `BindToViewModel` 经上一轮强化后，支持公有/私有属性与字段并给出可诊断错误，但**只解析唯一命名（`%`）节点**。
- 真实 UI 结构中大量节点没有唯一命名（容器下的同级控件、嵌套布局），仅靠 `%` 无法表达，必须在代码里手写 `GetNode(...)`。
- 使用者要求新增：**非唯一节点注入**、**相对路径**、**绝对路径**，并让名称能从**属性/字段名**派生（含 `_` 前缀字段）。
- 约束：服务注入与节点注入仍**相互独立**；`[UniqueNode]` 的既有语义不得破坏。
- 本变更尚未归档（`openspec/specs/` 为空），其 delta 未落盘，因此直接在本变更内修订规范，不产生跨变更的 MODIFIED。

## Goals / Non-Goals

**Goals:**
- 让节点绑定覆盖唯一命名、非唯一子节点、相对路径与绝对路径。
- 保持 `[UniqueNode]` 向后兼容，不改变其唯一命名语义。
- 让名派生对属性与字段都可用（处理 `_` 前缀惯例）。
- 保持两条管线独立、失败可诊断。

**Non-Goals:**
- 不合并服务注入与节点注入管线。
- 不做非成员目标的绑定（方法、构造参数、资源/脚本绑定）。
- 不引入「路径推断」（即不靠字符串形态猜唯一名 vs 路径）。
- 不改动 `GodotApi` 异步加载、`Dispatcher` 并发、日志 provider（见 proposal 的 Follow-ups）。

## Decisions

### D1. 服务注入与节点注入保持两条独立管线
`WireNode()`（`[Inject]`，容器解析）与节点绑定（`[UniqueNode]` / `[FromNode]`，场景树查找）保持为互不调用的独立入口。
- 理由：语义不同（容器解析 vs 场景树查找），失败原因与诊断维度不同；使用者已明确要求分开。
- 备选：单一 `WireNode()` 一次完成两者——被否决，排除。

### D2. 唯一命名与路径绑定拆成两个特性
`[UniqueNode]` 继续只做唯一命名（`%`）；新增 `[FromNode]` 接收**原样 NodePath**（子节点/相对/绝对/唯一/父级）。
- 理由：`[UniqueNode("Score")]` 当前语义是 `%Score`；若改为「原样路径」，`"Score"` 会变成子节点查找，属**破坏性变更**。拆开后语义自明、无需推断、向后兼容。
- 备选：单特性 + 字符串形态推断（`/` 开头=绝对、含 `/`=相对、其余=唯一名）——与 Godot 惯例相反，且**无法表达「名字中无斜杠的非唯一子节点」**，排除。
- 备选：给 `[UniqueNode]` 加 `Kind`/`bool` 开关——单特性塞两种语义，可读性差，排除。

### D3. 节点绑定的成员范围对齐 `[Inject]`
两种特性统一扫描 `BindingFlags.Instance | Public | NonPublic`，属性与字段一并处理，跳过索引器与不可写成员。
- 理由：与 `[Inject]` 的成员契约保持一致；「跑得起来但不生效」比显式失败更危险。
- 备选：仅公有属性（原始实现）——不满足目标，排除。

### D4. 名派生：去掉前导下划线
未显式指定名称/路径时，目标名由成员名派生并**去掉前导下划线**（`_score` → `score`），属性与字段同一规则。
- 理由：C# 私有字段惯例 `_camelCase` 与 Godot 唯一命名 `PascalCase`/`camelCase` 天然冲突；原样回退使 `[UniqueNode] private Label _score` 去找 `%_score`，几乎必然失败。
- 权衡：这是「未指定时」的行为变更。对无下划线前缀的成员（属性）无影响；对 `_` 前缀字段由「几乎必然失败」变为「命中 `camelCase`」。
- 备选：再尝试 PascalCase 变体（`_score` → `%score` 再试 `%Score`）——两个候选可能同时存在而产生歧义，排除；需要 PascalCase 命名时请显式指定。

### D5. 绑定失败给出可诊断错误
- 类型不符：`NodeBindingException`（成员名 + 期望类型 + 实际类型）。
- 目标缺失：默认 `NodeNotFoundException`，信息含**目标表达式、成员名与宿主类型**；`IgnoreInexistent = true` 时跳过。
- 理由：反射原生异常不含成员上下文，排查成本高。

### D6. `[FromNode]` 原样解析、不做推断
`[FromNode]` 的路径参数原样交给 `GetNodeOrNull`，不加前缀、不做形态推断；省略路径时回退成员名（去前导 `_`），按相对路径解析。
- 理由：与 Godot 自身的 NodePath 语义一致，学习成本为零；路径正确性由引擎保证。
- 备选：对省略路径默认加 `%`——与「路径」语义不符（`[FromNode]` 的默认应是相对路径），排除。

### D7. 默认注册补全采用 `TryAdd*` 语义
`AppHost.ConfigureServices` 以 `TryAdd*` 注册框架自带服务图（见 `host-default-registration`）。
- 理由：开箱可用且不妨碍覆盖；`TryAdd` 不覆盖已存在注册，符合「默认值可被替换」。
- 备注：`TryAddEnumerable` 不能用于「服务类型 == 实现类型」的工厂描述符（会抛 `ArgumentException`），因此 `IOnProcess → Dispatcher` 使用 `TryAddSingleton<IOnProcess>(工厂)`。

## Risks / Trade-offs

- [名派生行为变更影响既有用法] → 仅影响「未显式指定」且成员名含前导 `_` 的情形；以 README 与 release notes 说明，需要旧行为时显式指定名称。
- [绝对路径随场景结构变动而失效] → 属 Godot 固有语义；文档建议优先唯一命名/相对路径，绝对路径用于稳定引用。
- [私有成员绑定的反射开销] → 仅在调用 `BindToViewModel` 时执行一次，量级可忽略。
- [节点绑定在无引擎环境下难以端到端测试] → 纯逻辑（成员筛选、表达式生成、名派生）以单元测试覆盖；真实解析在样例中运行验证。

## Migration Plan

1. 新增 `FromNode` 特性；`NodeBindingPlan` 同时识别两种特性并产出 NodePath 表达式；名派生去前导 `_`。
2. `BindToViewModel` 统一按表达式解析，诊断信息带成员与宿主。
3. 扩展单元测试与 README。
4. 回归 `samples/GoDough.Demo`：同时演示 `[UniqueNode]`（`%Label`）与 `[FromNode]`（相对路径到非唯一按钮）。
5. 发布为次版本；release notes 注明 `[FromNode]` 新增与名派生变更。
- 回滚：改动以新增特性 + 派生规则调整为主，回退不涉及数据迁移。

## Open Questions

- 是否需要为 `[UniqueNode]` 补 `Required` 参数（与 `[Inject(Required)]` 对齐），还是沿用 `IgnoreInexistent`？倾向沿用现有参数以免两套语义。
- 名派生是否还需要支持「去掉 `_` 后再试 PascalCase」？（倾向不做，见 D4）
- 是否把 `AddGodotRuntimeUtilities` / `AddThreadingUtilities` / `AddComponentFactory` 标记为「可选、幂等」并长期保留？
