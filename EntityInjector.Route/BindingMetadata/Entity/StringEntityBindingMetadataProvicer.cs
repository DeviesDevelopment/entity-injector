using EntityInjector.Route.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace EntityInjector.Route.BindingMetadata.Entity;

public class StringEntityBindingMetadataProvider<TValue> : FromRouteToEntityBindingMetadataProvider<string, TValue>
{
    protected override string GetId(ActionContext context, string argumentName)
    {
        var routeValue = context.HttpContext.GetRouteValue(argumentName);

        if (routeValue == null)
            throw new MissingRouteParameterException(argumentName);

        return routeValue switch
        {
            string s when !string.IsNullOrWhiteSpace(s) => s,
            Guid g => g.ToString(),
            _ => throw new InvalidRouteParameterFormatException(argumentName, routeValue.GetType(), routeValue.GetType())
        };
    }
}