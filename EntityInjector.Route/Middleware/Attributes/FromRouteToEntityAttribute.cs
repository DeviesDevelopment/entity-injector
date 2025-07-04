namespace EntityInjector.Route.Middleware.Attributes;

[AttributeUsage(AttributeTargets.Parameter)]
public class FromRouteToEntityAttribute(string argumentName, string? metaData = null) : Attribute
{
    public readonly string ArgumentName = argumentName;
    public readonly Dictionary<string, string> MetaData = MetadataParsingHelper.ParseMetaData(metaData);
}