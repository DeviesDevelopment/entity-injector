using EntityInjector.Route.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace EntityInjector.Route.Middleware.BindingMetadata.Collection;

public class IntCollectionBindingMetadataProvider<TValue> : FromRouteToCollectionBindingMetadataProvider<int, TValue>
{
    protected override List<int> GetIds(ActionContext context, string argumentName)
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
                $"Route parameter '{argumentName}' is present but empty. Expected a comma-separated list of ints.");
        }

        var segments = rawString.Split(',');

        var invalidSegments = new List<string>();
        var parsedInts = new List<int>();

        foreach (var segment in segments)
        {
            if (int.TryParse(segment, out var parsed))
            {
                parsedInts.Add(parsed);
            }
            else
            {
                invalidSegments.Add(segment);
            }
        }

        if (invalidSegments.Any())
        {
            throw new InternalServerErrorException(
                $"The following values in route parameter '{argumentName}' are not valid ints: {string.Join(", ", invalidSegments)}.");
        }

        return parsedInts;
    }
}