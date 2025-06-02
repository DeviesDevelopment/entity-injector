using DbRouter.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace DbRouter.Middleware.BindingMetadata.Collection;

public class GuidCollectionBindingMetadataProvider<TValue> : FromRouteToCollectionBindingMetadataProvider<Guid, TValue>
{
    protected override List<Guid> GetIds(ActionContext context, string argumentName)
    {
        var routeValue = context.HttpContext.GetRouteValue(argumentName);

        if (routeValue == null)
        {
            throw new InternalServerErrorException(
                $"No route value found for parameter '{argumentName}'. Ensure it's included in the route.");
        }

        var rawString = routeValue.ToString();
        if (string.IsNullOrWhiteSpace(rawString))
        {
            throw new InternalServerErrorException(
                $"Route parameter '{argumentName}' is present but empty. Expected a comma-separated list of GUIDs.");
        }

        var segments = rawString.Split(',');

        var invalidSegments = new List<string>();
        var parsedGuids = new List<Guid>();

        foreach (var segment in segments)
        {
            if (Guid.TryParse(segment, out var parsed))
            {
                parsedGuids.Add(parsed);
            }
            else
            {
                invalidSegments.Add(segment);
            }
        }

        if (invalidSegments.Any())
        {
            throw new InternalServerErrorException(
                $"The following values in route parameter '{argumentName}' are not valid GUIDs: {string.Join(", ", invalidSegments)}.");
        }

        return parsedGuids;
    }
}