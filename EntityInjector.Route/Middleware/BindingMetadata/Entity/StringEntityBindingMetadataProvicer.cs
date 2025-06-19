using EntityInjector.Route.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace EntityInjector.Route.Middleware.BindingMetadata.Entity;

public class StringEntityBindingMetadataProvider<TValue> : FromRouteToEntityBindingMetadataProvider<string, TValue>
{
    protected override string GetId(ActionContext context, string argumentName)
    {
        var routeValue = context.HttpContext.GetRouteValue(argumentName);

        if (routeValue == null)
        {
            throw new InternalServerErrorException(
                $"Route parameter '{argumentName}' was not found. Ensure it is correctly specified in the route.");
        }

        return routeValue switch
        {
            string s when !string.IsNullOrWhiteSpace(s) => s,
            Guid g => g.ToString(),
            _ => throw new InternalServerErrorException(
                $"Route parameter '{argumentName}' must be a non-empty string or a Guid, but received type '{routeValue.GetType().Name}'.")
        };
    }
}