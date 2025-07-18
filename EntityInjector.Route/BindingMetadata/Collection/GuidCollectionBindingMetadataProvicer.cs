using EntityInjector.Core.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace EntityInjector.Route.BindingMetadata.Collection;

public class GuidCollectionBindingMetadataProvider<TValue> : FromRouteToCollectionBindingMetadataProvider<Guid, TValue>
{
    protected override List<Guid> GetIds(ActionContext context, string argumentName)
    {
        var routeValue = context.HttpContext.GetRouteValue(argumentName);

        if (routeValue == null)
            throw new MissingEntityParameterException(argumentName);

        var rawString = routeValue.ToString();
        if (string.IsNullOrWhiteSpace(rawString))
            throw new InvalidEntityParameterFormatException(argumentName, typeof(List<Guid>), typeof(string));

        var segments = rawString.Split(',');
        var invalidSegments = new List<string>();
        var parsedGuids = new List<Guid>();

        foreach (var segment in segments)
        {
            if (Guid.TryParse(segment, out var parsed))
                parsedGuids.Add(parsed);
            else
                invalidSegments.Add(segment);
        }

        if (invalidSegments.Any())
            throw new InvalidEntityParameterFormatException(
                argumentName,
                typeof(Guid),
                typeof(string)
            );

        return parsedGuids;
    }
}