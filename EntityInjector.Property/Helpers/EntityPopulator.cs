using System.Collections;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace EntityInjector.Property.Helpers;

internal static class EntityPopulator
{
    public static void Populate(
        object targetObject,
        PropertyInfo targetProperty,
        Type entityType,
        List<object?> ids,
        IDictionary fetchedEntities,
        ModelStateDictionary modelState,
        Dictionary<string, string> metaData)
    {
        if (ids.Count == 0) return;

        bool cleanNoMatch = metaData.ContainsKey("cleanNoMatch");
        bool includeNulls = metaData.ContainsKey("includeNulls");

        if (IsDictionaryType(targetProperty.PropertyType))
        {
            PopulateDictionary(targetObject, targetProperty, ids, fetchedEntities, modelState, cleanNoMatch, includeNulls);
        }
        else if (IsEnumerableButNotString(targetProperty.PropertyType))
        {
            PopulateList(targetObject, targetProperty, ids, fetchedEntities, modelState, cleanNoMatch, includeNulls);
        }
        else
        {
            PopulateSingle(targetObject, targetProperty, ids[0], fetchedEntities, modelState, cleanNoMatch, includeNulls);
        }
    }

    private static void PopulateDictionary(
        object targetObject,
        PropertyInfo prop,
        List<object?> ids,
        IDictionary fetchedEntities,
        ModelStateDictionary modelState,
        bool cleanNoMatch,
        bool includeNulls)
    {
        var dict = prop.GetValue(targetObject) as IDictionary 
                   ?? Activator.CreateInstance(prop.PropertyType) as IDictionary;

        dict?.Clear();

        foreach (var id in ids)
        {
            if (fetchedEntities.Contains(id))
            {
                dict![id!] = fetchedEntities[id!];
            }
            else if (includeNulls)
            {
                dict![id!] = null;
            }
            else if (!cleanNoMatch)
            {
                modelState.AddModelError(prop.Name, $"No matching entity found for ID '{id}'.");
            }
        }

        prop.SetValue(targetObject, dict);
    }

    private static void PopulateList(
        object targetObject,
        PropertyInfo prop,
        List<object?> ids,
        IDictionary fetchedEntities,
        ModelStateDictionary modelState,
        bool cleanNoMatch,
        bool includeNulls)
    {
        var matched = new List<object?>();

        foreach (var id in ids)
        {
            if (fetchedEntities.Contains(id))
            {
                matched.Add(fetchedEntities[id!]);
            }
            else if (includeNulls)
            {
                matched.Add(null);
            }
            else if (!cleanNoMatch)
            {
                modelState.AddModelError(prop.Name, $"No matching entity found for ID '{id}'.");
            }
        }

        prop.SetValue(targetObject, ConvertListToTargetType(matched, prop.PropertyType));
    }

    private static void PopulateSingle(
        object targetObject,
        PropertyInfo prop,
        object? id,
        IDictionary fetchedEntities,
        ModelStateDictionary modelState,
        bool cleanNoMatch,
        bool includeNulls)
    {
        if (fetchedEntities.Contains(id))
        {
            prop.SetValue(targetObject, fetchedEntities[id!]);
        }
        else if (includeNulls)
        {
            prop.SetValue(targetObject, null);
        }
        else if (!cleanNoMatch)
        {
            modelState.AddModelError(prop.Name, $"No matching entity found for ID '{id}'.");
        }
    }

    private static object? ConvertListToTargetType(List<object?> values, Type propType)
    {
        if (propType.IsArray)
        {
            var elementType = propType.GetElementType()!;
            var array = Array.CreateInstance(elementType, values.Count);
            for (int i = 0; i < values.Count; i++)
                array.SetValue(values[i], i);
            return array;
        }

        if (propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof(List<>))
        {
            var list = (IList)Activator.CreateInstance(propType)!;
            foreach (var v in values) list.Add(v);
            return list;
        }

        if (typeof(IEnumerable).IsAssignableFrom(propType))
        {
            var elementType = propType.GetGenericArguments().FirstOrDefault() ?? typeof(object);
            var listType = typeof(List<>).MakeGenericType(elementType);
            var list = (IList)Activator.CreateInstance(listType)!;
            foreach (var v in values) list.Add(v);
            return list;
        }

        return values;
    }

    private static bool IsDictionaryType(Type type) =>
        typeof(IDictionary).IsAssignableFrom(type) ||
        (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>));

    private static bool IsEnumerableButNotString(Type type) =>
        type != typeof(string) &&
        typeof(IEnumerable).IsAssignableFrom(type) &&
        !typeof(IDictionary).IsAssignableFrom(type);
}
