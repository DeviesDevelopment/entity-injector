using EntityInjector.Core.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace EntityInjector.Route.BindingMetadata.Entity;

public class IntEntityBindingMetadataProvider<TValue> : FromRouteToEntityBindingMetadataProvider<int, TValue>
{
    protected override int GetId(ActionContext context, string argumentName)
    {
        var routeValue = context.HttpContext.GetRouteValue(argumentName);

        if (routeValue == null)
            throw new MissingEntityParameterException(argumentName);

        return routeValue switch
        {
            int g => g,
            string s => int.Parse(s),
            _ => throw new InvalidEntityParameterFormatException(argumentName, routeValue.GetType(),
                routeValue.GetType())
        };
    }
}