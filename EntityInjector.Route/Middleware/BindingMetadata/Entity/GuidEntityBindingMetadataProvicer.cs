using EntityInjector.Route.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace EntityInjector.Route.Middleware.BindingMetadata.Entity;

public class GuidEntityBindingMetadataProvider<TValue> : FromRouteToEntityBindingMetadataProvider<Guid, TValue>
{
    protected override Guid GetId(ActionContext context, string argumentName)
    {
        var routeValue = context.HttpContext.GetRouteValue(argumentName);

        if (routeValue == null)
            throw new MissingRouteParameterException(argumentName);

        return routeValue switch
        {
            Guid g => g,
            string s when Guid.TryParse(s, out var parsed) => parsed,
            _ => throw new InvalidRouteParameterFormatException(argumentName, typeof(Guid), routeValue.GetType())
        };
    }
}