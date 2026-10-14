using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GoDough.Runtime {
  public static class IServiceCollectionExtensions {
    public static IServiceCollection AddGodotRuntimeUtilities(this IServiceCollection serviceCollection) {
      serviceCollection.TryAddSingleton<IGodotApi, GodotApi>();
      return serviceCollection;
    }
  }
}