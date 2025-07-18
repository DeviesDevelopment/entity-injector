using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace EntityInjector.Core.Exceptions.Middleware;

public class RouteBindingExceptionMiddleware(
    RequestDelegate next,
    ILogger<RouteBindingExceptionMiddleware> logger,
    IRouteBindingProblemDetailsFactory? problemDetailsFactory = null)
{
    private readonly IRouteBindingProblemDetailsFactory _problemDetailsFactory = problemDetailsFactory ?? new DefaultRouteBindingProblemDetailsFactory();
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (RouteBindingException ex)
        {
            logger.LogWarning(ex, "Route binding error: {Message}", ex.Message);

            var problemDetails = _problemDetailsFactory.Create(context, ex);

            context.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";

            var json = JsonSerializer.Serialize(problemDetails, JsonOptions);
            await context.Response.WriteAsync(json);
        }
    }

}
