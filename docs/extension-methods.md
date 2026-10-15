# Extension methods

Small helpers across common Godot types.

- [Aabb](#aabb)
- [Color](#color)
- [Resource](#resource)
- [Node](#node)
- [MeshInstance3D](#meshinstance3d)
- [Vector3](#vector3)

## Aabb

### `ConvertToMesh`

Converts bounding-box information into a mesh, e.g. for debugging — to see the exact extents of a 3D object.

```cs
var mesh = aabb.ConvertToMesh(Colors.Lime);
```

## Color

### `WithAlpha`

Copies the R/G/B of the color and adds an alpha channel, returning a new `Color`:

```cs
var translucent = color.WithAlpha(0.1f);
```

## Resource

### `DuplicateWith`

Creates a copy of a resource and applies modifications to it. Handy for deriving one style from another:

```cs
style.DuplicateWith<StyleBoxFlat>(copy =>
{
    copy.BgColor = Color.FromHtml("5c5c5c");
});
```

## Node

### `GetContentsBoundingBox`

On `List<MeshInstance3D>`: merges the rotated/transformed `Aabb`s of all listed meshes and returns the
absolute aggregation — the group's bounding box.

```cs
var boundingBox = nodes.GetContentsBoundingBox();
Vector3 size = boundingBox.Size;
```

### `ClearAllChildren`

Removes all children of the node.

### `FindAllChildren`

Recursively finds all descendants matching a predicate.

```cs
var visualInstances = sceneRoot
    .FindAllChildren(node => node is VisualInstance3D)
    .OfType<VisualInstance3D>();
```

## MeshInstance3D

### `GetGlobalAabb`

Returns the global-space `Aabb` of the mesh instance (a re-implementation of
[godotengine/godot#66940](https://github.com/godotengine/godot/pull/66940)).

### `GetRotatedAndTransformedAabb`

Returns the `Aabb` taking `GlobalRotation` and `GlobalTransform.Basis.Scale` into account.

## Vector3

### `Copy`

Returns a copy of the vector.

### `CopyWith`

Returns a copy with selected components overridden. Passing `null` keeps the source value.

```cs
Vector3 vector = new(1, 2, 3);
Vector3 second = vector.CopyWith(null, 5, null);   // 1, 5, 3
```