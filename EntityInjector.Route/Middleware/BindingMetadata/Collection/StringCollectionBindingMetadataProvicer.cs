using EntityInjector.Route.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace EntityInjector.Route.Middleware.BindingMetadata.Collection;

public class StringCollectionBindingMetadataProvider<TValue> : FromRouteToCollectionBindingMetadataProvider<string, TValue>
{
    protected override List<string> GetIds(ActionContext context, string argumentName)
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

        var segments = rawString
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        if (segments.Count == 0)
        {
            throw new InternalServerErrorException(
                $"Route parameter '{argumentName}' did not contain any valid string segments.");
        }

        return segments;
    }
}