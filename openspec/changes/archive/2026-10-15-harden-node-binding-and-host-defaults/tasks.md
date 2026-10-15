# Tasks

## 1. 节点绑定强化（保持独立管线）

- [x] 1.1 重写 `BindToViewModel` 的成员扫描：`BindingFlags.Instance | Public | NonPublic`，属性与字段一并处理，跳过索引器与不可写成员；验证公有/私有属性、公有/私有字段四类成员均被绑定
- [x] 1.2 保持 `%唯一命名` 语义：目标名 = `UniqueNode.SpecificName ?? 成员名`；验证「显式指定名」与「回退成员名」两种情形均命中正确节点
- [x] 1.3 成员类型与目标节点类型不符时抛出含「成员名 + 期望类型 + 实际类型」的诊断错误；验证错误信息足以直接定位问题
- [x] 1.4 缺失节点的必填/忽略语义：默认抛 `NodeNotFoundException`；`IgnoreInexistent = true` 时跳过不抛；验证两条路径
- [x] 1.5 确认与服务注入管线互不调用、可各自单独使用；验证「只做节点绑定」与「只做服务注入」均能独立工作

## 2. 宿主默认注册补全

- [x] 2.1 `AppHost.ConfigureServices` 默认注册 `AppHost` 自身、`IGodotApi`、`Dispatcher`（含 `IOnProcess` 映射）、`ComponentFactory`；验证不额外调用任何 `AddXxxUtilities` 即可解析 `SceneManagementService<>`
- [x] 2.2 默认注册采用 `TryAdd*` 语义；验证使用者覆盖某默认实现后，解析到的是使用者的实现
- [x] 2.3 复核既有 `AddGodotRuntimeUtilities` / `AddThreadingUtilities` / `AddComponentFactory` 与默认注册共存不冲突（重复注册的无害性）；验证同时调用与不调用两种情形均可解析

## 3. 测试与验证

- [x] 3.1 为节点绑定新增单元测试：公有/私有属性与字段、显式名与回退名、缺失必填抛错、`IgnoreInexistent` 跳过、类型不符报错；验证 `dotnet test` 通过
- [x] 3.2 为默认注册新增测试：开箱解析 `SceneManagementService<>`、使用者覆盖生效；验证通过
- [x] 3.3 更新 README：补充节点绑定用法（唯一命名、成员范围、诊断）与「开箱可用」的默认注册说明；验证文档含可运行示例

## 4. 样例回归（唯一命名）

- [x] 4.1 简化 `samples/GoDough.Demo`：删除为绕过默认注册缺失而手写的 `AddSingleton<AppHost>` / `AddXxxUtilities`，确认 demo 仍可运行；验证日志与场景切换与改造前一致

## 5. 路径化节点绑定（`[FromNode]`）

- [x] 5.1 新增 `FromNode` 特性：原样接受 NodePath 表达式（单段子节点 / 相对 / 绝对 / 唯一 / 父级），并提供 `ignoreInexistent`；验证特性可标注属性与字段
- [x] 5.2 `NodeBindingPlan` 同时识别 `[UniqueNode]` 与 `[FromNode]`（含派生特性），产出统一的 NodePath 表达式：唯一命名加 `%` 前缀，路径原样；验证两种特性的表达式生成正确
- [x] 5.3 名派生规则统一为**去掉前导下划线**（属性与字段同一规则）；验证 `_score` → `score`、`Score` → `Score`
- [x] 5.4 扩展单元测试：相对/绝对路径原样、`%` 表达式、省略路径回退、`_` 去前缀、派生特性、`ignoreInexistent` 传播；验证 `dotnet test` 通过
- [x] 5.5 更新 README：`[FromNode]` 用法（子节点/相对/绝对/唯一/父级）与名派生规则；验证文档含可运行示例
- [x] 5.6 样例同时演示两种特性：`[UniqueNode("Label")]`（`%Label`）与 `[FromNode("SwitchButton")]`（相对路径到**非唯一**按钮，已移除其唯一命名标记）；验证编译通过
- [x] 5.7 在 Godot 中运行样例，验证路径绑定与非唯一节点在真实场景树中生效；验证切换场景后绑定仍正确
