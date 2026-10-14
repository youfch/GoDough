using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GoDough.Visuals.Components {
  public static class IServiceCollectionExtensions {
    public static IServiceCollection AddComponentFactory(this IServiceCollection serviceCollection) {
      serviceCollection.TryAddSingleton<ComponentFactory>();
      return serviceCollection;
    }
  }
}