using System.Collections;
using System.Reflection;
using EntityInjector.Core.Exceptions;
using EntityInjector.Property.Attributes;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace EntityInjector.Property.Helpers;

public static class EntityBindingCollector
{
    public static List<EntityBindingInfo<TKey>> Collect<TKey>(object? root, ModelStateDictionary modelState)
    {
        var result = new List<EntityBindingInfo<TKey>>();
        Recurse(root, result, modelState);
        return result;
    }

    private static void Recurse<TKey>(
        object? currentObject,
        List<EntityBindingInfo<TKey>> toProcess,
        ModelStateDictionary modelState)
    {
        if (currentObject == null)
            return;

        var objType = currentObject.GetType();

        if (IsSimpleType(objType))
            return;

        if (currentObject is IEnumerable enumerable && objType != typeof(string))
        {
            foreach (var item in enumerable)
                Recurse(item, toProcess, modelState);

            return;
        }

        if (currentObject is IDictionary dict)
        {
            foreach (var key in dict.Keys)
            {
                var val = dict[key];
                Recurse(val, toProcess, modelState);
            }

            return;
        }

        foreach (var prop in objType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (prop.GetIndexParameters().Length > 0)
                continue;

            var attr = prop.GetCustomAttribute<FromPropertyToEntityAttribute>();
            if (attr != null)
            {
                var idProp = objType.GetProperty(
                    attr.PropertyName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (idProp == null)
                {
                    throw new MissingEntityAttributeException(
                        prop.Name,
                        $"Expected property '{attr.PropertyName}' to exist on '{prop.DeclaringType?.Name}' for attribute on '{prop.Name}");
                }

                var idValue = idProp.GetValue(currentObject);
                var ids = ExtractIds<TKey>(idValue);

                var entityType = prop.PropertyType.IsGenericType
                    ? prop.PropertyType.GenericTypeArguments.Last()
                    : prop.PropertyType;

                toProcess.Add(new EntityBindingInfo<TKey>
                {
                    TargetObject = currentObject,
                    TargetProperty = prop,
                    EntityType = entityType,
                    Ids = ids,
                    MetaData = attr.MetaData
                });
            }
            else
            {
                var nestedValue = prop.GetValue(currentObject);
                Recurse(nestedValue, toProcess, modelState);
            }
        }
    }

    private static List<TKey> ExtractIds<TKey>(object? idValue)
    {
        var result = new List<TKey>();
        if (idValue == null)
            return result;

        if (idValue is TKey single)
        {
            result.Add(single);
        }
        else if (idValue is IEnumerable enumerable && !(idValue is string))
        {
            foreach (var item in enumerable)
            {
                if (item is TKey tk)
                    result.Add(tk);
            }
        }
        else if (idValue is IDictionary dict)
        {
            foreach (var key in dict.Keys)
            {
                if (key is TKey tk)
                    result.Add(tk);
            }
        }

        return result;
    }

    private static bool IsSimpleType(Type type)
    {
        return type.IsPrimitive
               || type.IsEnum
               || type == typeof(string)
               || type == typeof(decimal)
               || type == typeof(DateTime)
               || type == typeof(DateTimeOffset)
               || type == typeof(TimeSpan)
               || type == typeof(Guid);
    }
}
