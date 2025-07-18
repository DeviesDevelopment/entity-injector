using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EntityInjector.Core.Exceptions.Middleware;

public interface IEntityBindingProblemDetailsFactory
{
    ProblemDetails Create(HttpContext context, EntityBindingException exception);
}