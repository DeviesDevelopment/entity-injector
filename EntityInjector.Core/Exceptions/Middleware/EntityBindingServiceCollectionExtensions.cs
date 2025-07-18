using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EntityInjector.Core.Exceptions.Middleware;

public static class EntityBindingServiceCollectionExtensions
{
    public static IServiceCollection AddRouteBinding(this IServiceCollection services)
    {
        // Register default formatter if user hasn't already
        services.TryAddSingleton<IEntityBindingProblemDetailsFactory, DefaultEntityBindingProblemDetailsFactory>();
        return services;
    }
}
