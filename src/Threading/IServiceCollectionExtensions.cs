using GoDough.Runtime.LivecycleHooks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GoDough.Threading {
  public static class IServiceCollectionExtensions {
    public static IServiceCollection AddThreadingUtilities(this IServiceCollection serviceCollection) {
      serviceCollection.TryAddSingleton<Dispatcher>();
      serviceCollection.TryAddSingleton<IOnProcess>(x => x.GetRequiredService<Dispatcher>());

      return serviceCollection;
    }
  }
}