using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EntityInjector.Route.Exceptions.Middleware;

public interface IRouteBindingProblemDetailsFactory
{
    ProblemDetails Create(HttpContext context, RouteBindingException exception);
}