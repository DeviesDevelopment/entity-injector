using System.Reflection;

namespace EntityInjector.Property.Helpers;

/// <summary>
///     Class for storing info about a single property that needs entity binding.
/// </summary>
public class EntityBindingInfo<TKey>
{
    public object TargetObject { get; set; } = default!;
    public PropertyInfo TargetProperty { get; set; } = default!;
    public Type EntityType { get; set; } = default!;
    public List<TKey> Ids { get; set; } = new();
    public Dictionary<string, string> MetaData { get; set; } = new();
}