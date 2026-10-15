# 3D bodies (Model3D)

`GoDough.Visuals.Model3D` is a `Node3D` that wraps a 3D model and can compute and visualise its
bounding box.

- [Properties](#properties)
- [Methods](#methods)
- [Example](#example)

## Properties

| Property | Description |
|---|---|
| `Node3D Model` | the model node to wrap; assigning it replaces the current children (except the internal bounding-box visual) |
| `Vector3 Size` | computed size of the bounding box; computed lazily on first access |
| `Aabb BoundingBox` | computed bounding box; computed lazily on first access |
| `bool DrawBoundingBox` | toggles the bounding-box visual |
| `Color DrawnBoundingBoxColor` | colour of the bounding-box visual (default: red) |
| `string GroupName` | group used to find the meshes to measure (default: `"Body"`) |
| `SizeCalculationMode CalculationMode` | `Group` (default) measures all grouped meshes; `Children` uses the single child mesh |

## Methods

| Method | Description |
|---|---|
| `void UpdateBoundingBox()` | recomputes `BoundingBox` / `Size` |
| `void ColorizeMeshGroup<T>(T group, Color color)` | sets `albedo_color` on the meshes in a group (`T` is an enum) |
| `void AddShaderOverlay<T>(T group, Shader shader, Dictionary<string, Variant> shaderParams = null)` | applies a shader overlay to the meshes in a group |
| `void RemoveShaderOverlay<T>(T group)` | removes the shader overlay from the meshes in a group |

## Example

```cs
using Godot;
using GoDough.Visuals;

var body = new Model3D { Model = myModelNode, GroupName = "Body" };
AddChild(body);

// Measure
Vector3 size = body.Size;

// Visualise
body.DrawnBoundingBoxColor = Colors.Lime;
body.DrawBoundingBox = true;
```

> Group membership is what ties meshes to the wrapper: put your meshes in the same Godot group
> (`GroupName`) for `CalculationMode.Group`, or use the `Children` mode to measure the single child
> mesh directly.