# Node bindings

- [What it is](#what-it-is)
- [`[UniqueNode]` — unique-named nodes](#uniquenode--unique-named-nodes)
- [`[FromNode]` — verbatim node paths](#fromnode--verbatim-node-paths)
- [Name derivation](#name-derivation)
- [Binding members of another class](#binding-members-of-another-class)
- [Diagnostics](#diagnostics)

## What it is

Node binding lets you declare node references as members instead of calling `GetNode(...)` yourself.

It is a **separate pipeline from service injection**: `WireNode()` resolves services from the
container, `BindToViewModel()` resolves nodes from the scene tree. They never call each other — call
whichever you need.

```cs
using Godot;
using GoDough.Composition.Extensions;   // WireNode
using GoDough.Visuals.Attributes;       // [UniqueNode] / [FromNode]
using GoDough.Visuals.Components;       // BindToViewModel

public partial class PlayerHud : Control
{
    [Inject] private IGameState _gameState = null!;   // service

    [UniqueNode] private Label _score = null!;        // %score   (see name derivation)

    public override void _Ready()
    {
        this.WireNode();        // services
        this.BindToViewModel(); // nodes
    }
}
```

> `BindToViewModel` lives in `GoDough.Visuals.Components` — remember that `using`.

## `[UniqueNode]` — unique-named nodes

Resolves the node marked as *unique name* (`%`) in the scene.

| Declaration | Resolves |
|---|---|
| `[UniqueNode]` | `%` + derived member name |
| `[UniqueNode("Score")]` | `%Score` |
| `[UniqueNode("Score", ignoreInexistent: true)]` | `%Score`, skipped when missing |

## `[FromNode]` — verbatim node paths

Passes the value to Godot's `NodePath` **verbatim**, so any expression Godot understands works:
a plain child, a relative path, an absolute path, a unique name, or a parent step.

| Declaration | Resolves |
|---|---|
| `[FromNode("Title")]` | child named `Title` (does **not** need to be unique) |
| `[FromNode("Hud/Score")]` | relative path |
| `[FromNode("/root/Main/Hud/Score")]` | absolute path |
| `[FromNode("%Score")]` | unique name |
| `[FromNode("..")]` | parent |
| `[FromNode]` | derived member name, as a relative path |
| `[FromNode("Title", ignoreInexistent: true)]` | skipped when missing |

## Name derivation

When no name/path is given, the target is derived from the member name with a **leading underscore
stripped** (`_score` → `score`). This applies to properties and fields alike, and to both attributes.

```
[UniqueNode] private Label _status  →  %status      (not %_status)
[UniqueNode]           Label Score  →  %Score
[FromNode]   private Label _title   →  title
```

> Godot node names are **case-sensitive**: `%score` and `%Score` are different nodes.

## Binding members of another class

You can keep view references in a separate class (a "view model") and bind it against a node:

```cs
public sealed class HudBinding
{
    [UniqueNode] public Label Score { get; set; } = null!;
}

public partial class PlayerHud : Control
{
    public override void _Ready()
    {
        var bindings = new HudBinding();
        this.BindToViewModel(bindings);   // binds HudBinding's members against this node
        bindings.Score.Text = "0";
    }
}
```

## Diagnostics

- **Missing node** (and not `ignoreInexistent`) throws
  `GoDough.Visuals.Components.NodeNotFoundException`, naming the path, the member and its owner.
- **Type mismatch** throws `GoDough.Visuals.Components.NodeBindingException`, reporting the member
  name, the expected type and the actual type.

Members that cannot be written (indexers, get-only properties, readonly fields) are skipped silently.