using Microsoft.AspNetCore.Builder;

namespace EntityInjector.Core.Exceptions.Middleware;

public static class RouteBindingApplicationBuilderExtensions
{
    public static IApplicationBuilder UseRouteBinding(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RouteBindingExceptionMiddleware>();
    }
}
