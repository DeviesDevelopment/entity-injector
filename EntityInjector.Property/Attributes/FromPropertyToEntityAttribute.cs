using EntityInjector.Core.Helpers;

namespace EntityInjector.Property.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class FromPropertyToEntityAttribute(string propertyName, string? metaData = null) : Attribute
{
    public readonly Dictionary<string, string> MetaData = MetadataParsingHelper.ParseMetaData(metaData);
    public readonly string PropertyName = propertyName;
}