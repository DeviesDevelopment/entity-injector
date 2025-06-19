using EntityInjector.Route.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace EntityInjector.Route.Middleware.BindingMetadata.Entity;

public class GuidEntityBindingMetadataProvider<TValue> : FromRouteToEntityBindingMetadataProvider<Guid,TValue>
{
    protected override Guid GetId(ActionContext context, string argumentName)
    {
        var routeValue = context.HttpContext.GetRouteValue(argumentName);

        if (routeValue == null)
        {
            throw new InternalServerErrorException(
                $"Route value for '{argumentName}' was not found. Make sure it is part of the route pattern.");
        }

        try
        {
            return routeValue switch
            {
                Guid g => g,
                string s when Guid.TryParse(s, out var parsed) => parsed,
                _ => throw new InvalidCastException(
                    $"Route value '{argumentName}' is neither a GUID nor a string that can be parsed into a GUID.")
            };
        }
        catch (Exception ex)
        {
            throw new InternalServerErrorException(
                $"Failed to parse route value '{argumentName}' as a GUID. Value: '{routeValue}'");
        }
    }
}