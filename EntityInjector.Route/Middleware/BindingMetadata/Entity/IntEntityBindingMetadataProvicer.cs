using EntityInjector.Route.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace EntityInjector.Route.Middleware.BindingMetadata.Entity;

public class IntEntityBindingMetadataProvider<TValue> : FromRouteToEntityBindingMetadataProvider<int, TValue>
{
    protected override int GetId(ActionContext context, string argumentName)
    {
        var routeValue = context.HttpContext.GetRouteValue(argumentName);

        if (routeValue == null)
            throw new MissingRouteParameterException(argumentName);

        return routeValue switch
        {
            int g => g,
            string s => int.Parse(s),
            _ => throw new InvalidRouteParameterFormatException(argumentName, routeValue.GetType(), routeValue.GetType())
        };
    }
}