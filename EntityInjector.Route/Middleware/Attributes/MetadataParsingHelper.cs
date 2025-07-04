namespace EntityInjector.Route.Middleware.Attributes;

public static class MetadataParsingHelper
{
    public static Dictionary<string, string> ParseMetaData(string? metaData)
    {
        if (string.IsNullOrEmpty(metaData)) return new Dictionary<string, string>();
        return metaData.Split("&")
            .Select(p => p.Split("="))
            .Where(p => p.Length == 2)
            .ToDictionary(p => p[0], p => p[1]);
    }
}