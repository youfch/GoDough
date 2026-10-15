# node-path-binding Specification

## Purpose
定义路径化节点绑定契约：`[FromNode]` 以 Godot NodePath 表达式（单段子节点、相对路径、绝对路径、唯一命名、父级）解析并绑定成员，用于覆盖非唯一命名节点与跨层级引用。

## Requirements

### Requirement: 原样 NodePath 解析

`[FromNode]` SHALL 把其路径参数作为 NodePath 表达式**原样**交给引擎解析，SHALL NOT 施加前缀、归一化或推断。支持的表达式包括：单段子节点名、相对路径、绝对路径、唯一命名（`%`）与父级（`..`）。

#### Scenario: 单段子节点（非唯一）
- **WHEN** 成员标注 `[FromNode("Title")]`
- **THEN** 绑定到当前节点的名为 `Title` 的子节点

#### Scenario: 相对路径
- **WHEN** 成员标注 `[FromNode("Hud/Score")]`
- **THEN** 绑定到相对当前节点解析出的 `Hud/Score`

#### Scenario: 绝对路径
- **WHEN** 成员标注 `[FromNode("/root/Main/Hud/Score")]`
- **THEN** 绑定到该绝对路径指向的节点

#### Scenario: 唯一命名表达式
- **WHEN** 成员标注 `[FromNode("%Score")]`
- **THEN** 绑定到唯一命名 `Score` 的节点

### Requirement: 省略路径时由成员名派生

当 `[FromNode]` 未提供路径时，绑定目标 SHALL 由成员名派生（去掉前导下划线），并作为相对路径解析。

#### Scenario: 省略路径
- **WHEN** 成员 `_title` 标注 `[FromNode]` 且未提供路径
- **THEN** 以派生名 `title` 作为相对路径查找并绑定

### Requirement: 与唯一命名绑定共享绑定语义

`[FromNode]` SHALL 与 `[UniqueNode]` 共享绑定语义：可绑定成员范围（公有/私有属性与字段，跳过索引器与不可写成员）、缺失诊断、类型不符诊断、缺失可忽略，以及「与服务注入相互独立」。跨特性差异仅在于目标解析方式。

#### Scenario: 成员范围一致
- **WHEN** 成员为私有字段（可写）并标注 `[FromNode]`
- **THEN** 该字段被绑定，语义与 `[UniqueNode]` 私有字段一致

#### Scenario: 缺失必填报错
- **WHEN** 目标节点缺失且未声明可忽略
- **THEN** 抛出包含目标表达式、成员名与宿主类型的错误

#### Scenario: 类型不符报错
- **WHEN** 目标节点类型与成员类型不符
- **THEN** 抛出包含成员名、期望类型与实际类型的错误

#### Scenario: 缺失可忽略
- **WHEN** 成员声明缺失可忽略且目标节点不存在
- **THEN** 绑定跳过该成员且不抛出异常

#### Scenario: 不触发服务解析
- **WHEN** 只执行节点绑定而不执行服务注入
- **THEN** `[FromNode]` 成员被正确绑定且不访问服务容器
