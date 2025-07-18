using Microsoft.AspNetCore.Builder;

namespace EntityInjector.Core.Exceptions.Middleware;

public static class EntityBindingApplicationBuilderExtensions
{
    public static IApplicationBuilder UseRouteBinding(this IApplicationBuilder app)
    {
        return app.UseMiddleware<EntityBindingExceptionMiddleware>();
    }
}