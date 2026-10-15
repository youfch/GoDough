# GoDough Documentation

Utility library for implementing state-of-the-art application development in Godot 4 (.NET).

- **.NET standards**
  - Logging via `ILogger` (`Microsoft.Extensions.Logging`)
  - Dependency injection (`Microsoft.Extensions.DependencyInjection`), with a replaceable container
- **Godot utilities**
  - Application host and lifetime
  - Service injection into nodes (`[Inject]`) and node binding (`[UniqueNode]` / `[FromNode]`)
  - Scene management, components, lifecycle hooks, main-thread dispatcher, 3D helpers

Targets **.NET 8** and **`Godot.NET.Sdk 4.7.2`**.

## Pages

1. [Getting started](./getting-started.md)
2. [Dependency injection](./dependency-injection.md)
3. [Logging](./logging.md)
4. [Node bindings](./node-bindings.md)
5. [Godot components](./components.md)
6. [Lifecycle hooks](./lifecycle-hooks.md)
7. [Threading](./threading.md)
8. [Utilities](./utilities.md)
9. [Extension methods](./extension-methods.md)
10. [3D bodies (Model3D)](./model-3d.md)

> This documentation lives in the repository (see `docs/`), so it is versioned with the code and
> reviewed in the same pull request as the changes it describes.