using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace EntityInjector.Core.Exceptions.Middleware;

public class EntityBindingExceptionMiddleware(
    RequestDelegate next,
    ILogger<EntityBindingExceptionMiddleware> logger,
    IEntityBindingProblemDetailsFactory? problemDetailsFactory = null)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IEntityBindingProblemDetailsFactory _problemDetailsFactory =
        problemDetailsFactory ?? new DefaultEntityBindingProblemDetailsFactory();

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (EntityBindingException ex)
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