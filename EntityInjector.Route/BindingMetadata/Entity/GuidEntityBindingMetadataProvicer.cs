using EntityInjector.Core.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace EntityInjector.Route.BindingMetadata.Entity;

public class GuidEntityBindingMetadataProvider<TValue> : FromRouteToEntityBindingMetadataProvider<Guid, TValue>
{
    protected override Guid GetId(ActionContext context, string argumentName)
    {
        var routeValue = context.HttpContext.GetRouteValue(argumentName);

        if (routeValue == null)
            throw new MissingEntityParameterException(argumentName);

        return routeValue switch
        {
            Guid g => g,
            string s when Guid.TryParse(s, out var parsed) => parsed,
            _ => throw new InvalidEntityParameterFormatException(argumentName, typeof(Guid), routeValue.GetType())
        };
    }
}