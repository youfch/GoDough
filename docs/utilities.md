# Utilities

- [Waiting for the next frame](#waiting-for-the-next-frame)
- [IGodotApi](#igodotapi)

## Waiting for the next frame

`WaitForNextFrame` waits until the next `process_frame`. Useful when a calculation only becomes
available after a frame has been rendered.

```cs
using Godot;
using GoDough.Runtime.Extensions;

public partial class MyPopup : Panel
{
    public override void _Ready() => _ = NotifyAfterFrameAsync();

    private async Task NotifyAfterFrameAsync()
    {
        await this.WaitForNextFrame();

        // Runs after the next frame has been rendered.
    }
}
```

## IGodotApi

`IGodotApi` abstracts async/sync loading of Godot resources. It is registered by default; inject it
where you need to load scenes or shaders.

| Method | Description |
|---|---|
| `PackedScene LoadScene(string fileName)` | loads a `*.tscn` synchronously |
| `TaskWithProgress<PackedScene> LoadSceneAsync(string fileName)` | same, reporting progress |
| `Shader LoadShader(string fileName)` | loads a `*.gdshader` synchronously |
| `TaskWithProgress<Shader> LoadShaderAsync(string fileName)` | same, reporting progress |

```cs
PackedScene LoadScene(string fileName);
TaskWithProgress<PackedScene> LoadSceneAsync(string fileName);
Shader LoadShader(string fileName);
TaskWithProgress<Shader> LoadShaderAsync(string fileName);
```

Requests are cached per file name: loading the same resource twice returns the first result instead of
reading from disk again.

`TaskWithProgress<T>` is a `Task<T>` with a `Progress<double>` attached, which makes it easy to drive a
progress bar while loading.

> Typically you do not call `IGodotApi` directly for scenes: `SceneManagementService<T>` wraps it and
> handles scene registration, caching and switching. See `src/Visuals/SceneManagementService.cs`.