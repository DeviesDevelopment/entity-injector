using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EntityInjector.Core.Exceptions.Middleware;

public class DefaultEntityBindingProblemDetailsFactory : IEntityBindingProblemDetailsFactory
{
    public ProblemDetails Create(HttpContext context, EntityBindingException exception)
    {
        return new ProblemDetails
        {
            Status = exception.StatusCode,
            Detail = exception.Message,
            Instance = context.Request.Path
        };
    }
}