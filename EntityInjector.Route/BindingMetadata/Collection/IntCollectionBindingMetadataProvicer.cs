using EntityInjector.Core.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace EntityInjector.Route.BindingMetadata.Collection;

public class IntCollectionBindingMetadataProvider<TValue> : FromRouteToCollectionBindingMetadataProvider<int, TValue>
{
    protected override List<int> GetIds(ActionContext context, string argumentName)
    {
        var routeValue = context.HttpContext.GetRouteValue(argumentName);

        if (routeValue == null)
            throw new MissingEntityParameterException(argumentName);

        var rawString = routeValue.ToString();
        if (string.IsNullOrWhiteSpace(rawString))
            throw new InvalidEntityParameterFormatException(argumentName, typeof(List<int>), typeof(string));

        var segments = rawString.Split(',');
        var invalidSegments = new List<string>();
        var parsedInts = new List<int>();

        foreach (var segment in segments)
            if (int.TryParse(segment, out var parsed))
                parsedInts.Add(parsed);
            else
                invalidSegments.Add(segment);

        if (invalidSegments.Any())
            throw new InvalidEntityParameterFormatException(
                argumentName,
                typeof(int),
                typeof(string)
            );

        return parsedInts;
    }
}