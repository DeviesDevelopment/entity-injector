using EntityInjector.Core.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace EntityInjector.Route.BindingMetadata.Collection;

public class
    StringCollectionBindingMetadataProvider<TValue> : FromRouteToCollectionBindingMetadataProvider<string, TValue>
{
    protected override List<string> GetIds(ActionContext context, string argumentName)
    {
        var routeValue = context.HttpContext.GetRouteValue(argumentName);

        if (routeValue == null)
            throw new MissingEntityParameterException(argumentName);

        var rawString = routeValue.ToString();
        if (string.IsNullOrWhiteSpace(rawString))
            throw new InvalidEntityParameterFormatException(argumentName, typeof(string), routeValue.GetType());

        var segments = rawString
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        if (segments.Count == 0)
            throw new EmptyEntitySegmentListException(argumentName);

        return segments;
    }
}