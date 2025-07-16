using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EntityInjector.Route.Exceptions.Middleware;

public class DefaultRouteBindingProblemDetailsFactory : IRouteBindingProblemDetailsFactory
{
    public ProblemDetails Create(HttpContext context, RouteBindingException exception)
    {
        return new ProblemDetails
        {
            Status = exception.StatusCode,
            Detail = exception.Message,
            Instance = context.Request.Path
        };
    }
}