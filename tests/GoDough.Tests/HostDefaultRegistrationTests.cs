using System.Linq;

using Godot;

using GoDough.Runtime;
using GoDough.Runtime.LivecycleHooks;
using GoDough.Threading;
using GoDough.Visuals;
using GoDough.Visuals.Components;

using Dispatcher = GoDough.Threading.Dispatcher;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GoDough.Tests;

internal enum DefaultRegistrationScene { Main }

internal sealed class FakeGodotApi : IGodotApi
{
    public Shader LoadShader(string fileName) => null!;

    public PackedScene LoadScene(string fileName) => null!;

    public TaskWithProgress<Shader> LoadShaderAsync(string fileName) => null!;

    public TaskWithProgress<PackedScene> LoadSceneAsync(string fileName) => null!;
}

public class HostDefaultRegistrationTests
{
    private static ServiceProvider BuildProvider(System.Action<IServiceCollection>? extra = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        // Provide the Godot API abstraction so the graph can be built without the engine.
        services.AddSingleton<IGodotApi>(new FakeGodotApi());

        new AppHost(null!).ConfigureServices(services);

        extra?.Invoke(services);

        return services.BuildServiceProvider();
    }

    [Fact]
    public void Scene_management_resolves_with_default_registration_only()
    {
        using var provider = BuildProvider();

        var service = provider.GetRequiredService<SceneManagementService<DefaultRegistrationScene>>();

        Assert.NotNull(service);
    }

    [Fact]
    public void Component_factory_resolves_by_default()
    {
        using var provider = BuildProvider();

        Assert.NotNull(provider.GetRequiredService<ComponentFactory>());
    }

    [Fact]
    public void Dispatcher_is_registered_as_a_process_hook_by_default()
    {
        using var provider = BuildProvider();

        Assert.Contains(provider.GetServices<IOnProcess>(), hook => hook is Dispatcher);
    }

    [Fact]
    public void Default_registration_does_not_override_user_registration()
    {
        var fake = new FakeGodotApi();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IGodotApi>(fake);

        new AppHost(null!).ConfigureServices(services);

        using var provider = services.BuildServiceProvider();

        Assert.Same(fake, provider.GetRequiredService<IGodotApi>());
    }

    [Fact]
    public void Optional_registration_helpers_are_idempotent()
    {
        using var provider = BuildProvider(services =>
            services.AddGodotRuntimeUtilities().AddThreadingUtilities().AddComponentFactory());

        Assert.Single(provider.GetServices<IOnProcess>().Where(hook => hook is Dispatcher));
    }

    [Fact]
    public void User_process_hook_coexists_with_dispatcher()
    {
        using var provider = BuildProvider(services => services.AddSingleton<IOnProcess, ProbeHook>());

        var hooks = provider.GetServices<IOnProcess>().ToList();

        Assert.Contains(hooks, hook => hook is Dispatcher);
        Assert.Contains(hooks, hook => hook is ProbeHook);
    }
}