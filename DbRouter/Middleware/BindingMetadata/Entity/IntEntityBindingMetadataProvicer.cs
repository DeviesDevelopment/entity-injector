using DbRouter.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace DbRouter.Middleware.BindingMetadata.Entity;

public class IntEntityBindingMetadataProvider<TValue> : FromRouteToEntityBindingMetadataProvider<int, TValue>
{
    protected override int GetId(ActionContext context, string argumentName)
    {
        var routeValue = context.HttpContext.GetRouteValue(argumentName);

        if (routeValue == null)
        {
            throw new InternalServerErrorException(
                $"Route parameter '{argumentName}' was not found. Ensure it is correctly specified in the route.");
        }

        return routeValue switch
        {
            int g => g,
            string s => int.Parse(s),
            _ => throw new InternalServerErrorException(
                $"Route parameter '{argumentName}' must be a non-empty string or an int, but received type '{routeValue.GetType().Name}'.")
        };
    }
}