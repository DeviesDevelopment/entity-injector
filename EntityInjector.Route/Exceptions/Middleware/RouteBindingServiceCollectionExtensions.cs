using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EntityInjector.Route.Exceptions.Middleware;

public static class RouteBindingServiceCollectionExtensions
{
    public static IServiceCollection AddRouteBinding(this IServiceCollection services)
    {
        // Register default formatter if user hasn't already
        services.TryAddSingleton<IRouteBindingProblemDetailsFactory, DefaultRouteBindingProblemDetailsFactory>();
        return services;
    }
}
