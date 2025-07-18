using System.Collections;
using System.Reflection;
using EntityInjector.Core.Interfaces;
using EntityInjector.Property.Attributes;
using EntityInjector.Property.Exceptions;
using EntityInjector.Property.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;

namespace EntityInjector.Property.Filters;

public abstract class FromPropertyToEntityActionFilter<TKey>(
    IServiceProvider serviceProvider,
    ILogger<FromPropertyToEntityActionFilter<TKey>> logger)
    : IAsyncActionFilter
{
    
    /// <summary>
    /// Converts an object (from reflection) into a typed key (e.g. Guid, int).
    /// This is the only method that subclasses must implement.
    /// </summary>
    protected abstract TKey ConvertToKey(object rawValue);

    /// <summary>
    /// The sentinel value used when a nullable property has no value.
    /// For example: Guid.Empty or 0
    /// </summary>
    protected abstract TKey GetDefaultValueForNull();

    protected virtual TKey GetId(ActionExecutingContext context, PropertyInfo propInfo, object objectData)
    {
        var idValue = propInfo.GetValue(objectData);
        var isMarkedAsNullable = NullableReflectionHelper.IsNullable(propInfo);

        return idValue switch
        {
            null when isMarkedAsNullable => GetDefaultValueForNull(),
            null => throw new ArgumentNullException($"{propInfo.Name} was null but not marked nullable"),
            _ => ConvertToKey(idValue)
        };
    }

    protected virtual List<TKey> GetIds(ActionExecutingContext context, PropertyInfo propInfo, IEnumerable listData)
    {
        var ids = new List<TKey>();
        var isMarkedAsNullable = NullableReflectionHelper.IsNullable(propInfo);

        foreach (var item in listData)
        {
            if (item == null)
            {
                if (!isMarkedAsNullable)
                    throw new ArgumentNullException($"{propInfo.Name} contained null but is not nullable");

                ids.Add(GetDefaultValueForNull());
            }
            else
            {
                ids.Add(ConvertToKey(item));
            }
        }

        return ids;
    }

    /// <summary>
    /// The main entry point for the filter. We gather all [FromPropertyToEntity] usages,
    /// collect IDs, batch-fetch entities, and then populate properties.
    /// </summary>
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.ActionArguments == null || context.ActionArguments.Count == 0)
        {
            await next();
            return;
        }

        // Step 1: Collect all info about properties that need entity binding.
        var toProcess = new List<EntityBindingInfo>();

        foreach (var arg in context.ActionArguments.Values)
        {
            if (arg != null)
            {
                CollectEntityBindings(arg, toProcess, context.ModelState);
            }
        }

        // Step 2: Group by (EntityType, MetaData) for batched DB calls
        var groupedByType = toProcess
            .GroupBy(info =>
                (info.EntityType, string.Join("&", info.MetaData.OrderBy(x => x.Key).Select(x => $"{x.Key}={x.Value}"))))
            .Select(g => new { EntityType = g.Key.EntityType, MetaData = g.First().MetaData, Bindings = g.ToList() })
            .ToList();

        // Step 3: For each group, gather all unique IDs and do a single DB call.
        foreach (var group in groupedByType)
        {
            // gather all IDs from the group
            var allIds = group.Bindings.SelectMany(b => b.Ids).Distinct().ToList();
            if (allIds.Count == 0)
            {
                continue; // skip if no IDs
            }

            IDictionary? fetchedEntities;
            try
            {
                // Use the method (already in your abstract class) that fetches entities in bulk
                fetchedEntities = await GetEntitiesAsync(allIds, context.HttpContext, group.EntityType, group.MetaData);
            }
            catch (Exception ex)
            {
                // If there's an error, log it and continue. We won't be able to populate these props
                logger.LogError(ex, "Error fetching entities for type {TypeName}", group.EntityType.Name);
                continue;
            }

            // Step 4: Populate each property that needs these entities
            foreach (var binding in group.Bindings)
            {
                PopulateEntitiesInProperty(binding, fetchedEntities, context.ModelState);
            }
        }

        // Step 5: Proceed to the next stage in the pipeline
        await next();
    }

    /// <summary>
    /// This method does the actual reflection-based recursion: 
    /// it looks for properties with [FromPropertyToEntity], extracts the ID(s),
    /// and stores them in the toProcess list.
    /// </summary>
    private void CollectEntityBindings(
        object? currentObject,
        List<EntityBindingInfo> toProcess,
        ModelStateDictionary modelState)
    {
        if (currentObject == null)
        {
            return;
        }

        var objType = currentObject.GetType();

        // If simple type or string, no further recursion
        if (IsSimpleType(objType))
        {
            return;
        }

        // If IEnumerable (and not string), recurse into each element
        if (typeof(IEnumerable).IsAssignableFrom(objType) && objType != typeof(string))
        {
            if (currentObject is IEnumerable enumerable)
            {
                foreach (var item in enumerable)
                {
                    CollectEntityBindings(item, toProcess, modelState);
                }
            }

            return;
        }

        // If IDictionary, recurse into each value
        if (typeof(IDictionary).IsAssignableFrom(objType))
        {
            if (currentObject is IDictionary dict)
            {
                foreach (var key in dict.Keys)
                {
                    var val = dict[key];
                    CollectEntityBindings(val, toProcess, modelState);
                }
            }

            return;
        }

        // Otherwise, it’s a complex object => reflect properties
        foreach (var prop in objType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            // skip indexers
            if (prop.GetIndexParameters().Length > 0)
            {
                continue;
            }

            // check for [FromPropertyToEntity]
            var fromPropAttr = prop.GetCustomAttribute<FromPropertyToEntityAttribute>();
            if (fromPropAttr != null)
            {
                // The annotated property 'prop' is where we'll store the fetched entity(ies).
                // 1) Find the ID property
                var idProp = objType.GetProperty(
                    fromPropAttr.PropertyName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (idProp == null)
                {
                    throw new CustomAttributeFormatException(
                        $"Bad configuration for attribute on {prop.Name}, no property with name {fromPropAttr.PropertyName}");
                }

                // 2) Read its value to extract IDs
                var idValue = idProp.GetValue(currentObject);
                var ids = ExtractIdsFromPropertyValue(idValue);

                // 3) The property type we want to fill
                var entityType = prop.PropertyType;
                if (entityType.IsGenericType)
                {
                    entityType = entityType.GenericTypeArguments.Last();
                }

                // 4) Create our binding record
                var bindingInfo = new EntityBindingInfo
                {
                    TargetObject = currentObject,
                    TargetProperty = prop,
                    EntityType = entityType,
                    Ids = ids,
                    MetaData = fromPropAttr.MetaData,
                };

                toProcess.Add(bindingInfo);

                // If needed, we could also recurse inside the property being set (prop) 
                // if that property might contain further [FromPropertyToEntity]. 
                // E.g., `CollectEntityBindings(prop.GetValue(currentObject), ...)`
                // But typically, that property is the “entity placeholder” (not filled yet).
            }
            else
            {
                // Recurse deeper if no attribute here
                var propValue = prop.GetValue(currentObject);
                CollectEntityBindings(propValue, toProcess, modelState);
            }
        }
    }

    /// <summary>
    /// Populates a single property with the entities fetched from the dictionary.
    /// Supports single entity, list, or dictionary.
    /// </summary>
    private void PopulateEntitiesInProperty(
        EntityBindingInfo binding,
        IDictionary? fetchedEntities,
        ModelStateDictionary modelState)
    {
        if (fetchedEntities == null)
        {
            return;
        }

        var propertyType = binding.TargetProperty.PropertyType;
        var hasCleanNoMatch = binding.MetaData.ContainsKey("cleanNoMatch");
        var includeNulls = binding.MetaData.ContainsKey("includeNulls");
        var ids = binding.Ids;

        // No IDs => nothing to do
        if (ids.Count == 0)
        {
            return;
        }

        if (IsDictionaryType(propertyType))
        {
            PopulateDictionaryProperty(binding, fetchedEntities, modelState, hasCleanNoMatch, includeNulls);
        }
        else if (IsEnumerableButNotString(propertyType))
        {
            // It's a list/array/IEnumerable
            PopulateListProperty(binding, fetchedEntities, modelState, hasCleanNoMatch, includeNulls);
        }
        else
        {
            // Otherwise, treat as a single entity
            PopulateSingleEntityProperty(binding, fetchedEntities, modelState, hasCleanNoMatch, includeNulls);
        }
    }

    private void PopulateDictionaryProperty(
        EntityBindingInfo binding,
        IDictionary fetchedEntities,
        ModelStateDictionary modelState,
        bool hasCleanNoMatch,
        bool includeNulls)
    {
        // We assume the property is IDictionary, e.g. Dictionary<TKey, TEntity>.
        // Create or clear existing
        var originalDict = binding.TargetProperty.GetValue(binding.TargetObject) as IDictionary;
        if (originalDict == null)
        {
            // Attempt to create instance of that dictionary type
            originalDict = Activator.CreateInstance(binding.TargetProperty.PropertyType) as IDictionary;
        }
        else
        {
            originalDict.Clear();
        }

        // For each ID, populate the dictionary
        foreach (var dictId in binding.Ids)
        {
            if (fetchedEntities.Contains(dictId))
            {
                originalDict![dictId] = fetchedEntities[dictId];
                continue;
            }

            if (includeNulls)
            {
                originalDict![dictId] = null;
            }
            else if (!hasCleanNoMatch)
            {
                modelState.AddModelError(
                    binding.TargetProperty.Name,
                    $"No matching entity found for ID '{dictId}'.");
            }
        }


        binding.TargetProperty.SetValue(binding.TargetObject, originalDict);
    }

    private void PopulateListProperty(
        EntityBindingInfo binding,
        IDictionary fetchedEntities,
        ModelStateDictionary modelState,
        bool hasCleanNoMatch,
        bool includeNulls)
    {
        var matchedEntities = new List<object>();

        foreach (var id in binding.Ids)
        {
            if (fetchedEntities.Contains(id))
            {
                matchedEntities.Add(fetchedEntities[id]!);
                continue;
            }

            if (includeNulls)
            {
                matchedEntities.Add(null);
            }
            else if (!hasCleanNoMatch)
            {
                modelState.AddModelError(
                    binding.TargetProperty.Name,
                    $"No matching entity found for ID '{id}'.");
            }
        }


        // Convert the gathered objects into the actual list/array type
        var finalValue = ConvertListToPropertyType(matchedEntities, binding.TargetProperty.PropertyType);
        binding.TargetProperty.SetValue(binding.TargetObject, finalValue);
    }


    private void PopulateSingleEntityProperty(
        EntityBindingInfo binding,
        IDictionary fetchedEntities,
        ModelStateDictionary modelState,
        bool hasCleanNoMatch,
        bool includeNulls)
    {
        // If the target property is single, but IDs has multiple,
        // you might only take the first or raise an error.
        // Usually you expect exactly one ID for a single-entity property.
        var id = binding.Ids[0];

        if (fetchedEntities.Contains(id))
        {
            binding.TargetProperty.SetValue(binding.TargetObject, fetchedEntities[id]);
        }
        else
        {
            if (!hasCleanNoMatch)
            {
                modelState.AddModelError(
                    binding.TargetProperty.Name,
                    $"No matching entity found for ID '{id}'.");
            } else if (includeNulls)
            {
                binding.TargetProperty.SetValue(binding.TargetObject, null);
            }
        }
    }

    private bool IsDictionaryType(Type type)
    {
        return typeof(IDictionary).IsAssignableFrom(type)
               || (type.IsGenericType
                   && typeof(Dictionary<,>) == type.GetGenericTypeDefinition());
    }

    private bool IsEnumerableButNotString(Type type)
    {
        if (type == typeof(string))
        {
            return false;
        }

        if (typeof(IEnumerable).IsAssignableFrom(type) && !typeof(IDictionary).IsAssignableFrom(type))
        {
            return true;
        }

        return false;
    }

    private List<TKey> ExtractIdsFromPropertyValue(object? idValue)
    {
        var result = new List<TKey>();
        if (idValue == null)
        {
            return result;
        }

        // Single TKey
        if (idValue is TKey single)
        {
            result.Add(single);
        }
        // IEnumerable
        else if (idValue is IEnumerable enumerable && !(idValue is string))
        {
            foreach (var item in enumerable)
            {
                if (item is TKey tk)
                {
                    result.Add(tk);
                }
            }
        }
        // IDictionary
        else if (idValue is IDictionary dict)
        {
            foreach (var key in dict.Keys)
            {
                if (key is TKey tk)
                {
                    result.Add(tk);
                }
            }
        }

        return result;
    }

    private bool IsSimpleType(Type type)
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

    /// <summary>
    /// Convert a generic List&lt;object&gt; to the property’s actual type
    /// (e.g., List&lt;T&gt;, T[], or IEnumerable&lt;T&gt;)
    /// </summary>
    private object? ConvertListToPropertyType(List<object> matchedEntities, Type propType)
    {
        // If it's an array
        if (propType.IsArray)
        {
            var elementType = propType.GetElementType()!;
            var arrayInstance = Array.CreateInstance(elementType, matchedEntities.Count);
            for (int i = 0; i < matchedEntities.Count; i++)
            {
                arrayInstance.SetValue(matchedEntities[i], i);
            }

            return arrayInstance;
        }

        // If it's a concrete List<T>
        if (propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof(List<>))
        {
            var listInstance = Activator.CreateInstance(propType) as IList;
            foreach (var entity in matchedEntities)
            {
                listInstance!.Add(entity);
            }

            return listInstance;
        }

        // If it implements IEnumerable<T> or ICollection<T>, 
        // we can still return a List<T>, or you might want to do more advanced reflection
        var genericArgs = propType.GetGenericArguments();
        if (genericArgs.Length == 1)
        {
            var listType = typeof(List<>).MakeGenericType(genericArgs[0]);
            var listInstance = Activator.CreateInstance(listType) as IList;
            foreach (var entity in matchedEntities)
            {
                listInstance!.Add(entity);
            }

            // If the property is just IEnumerable<T>, the List<T> assignment should be fine,
            // or you can do `.ToArray()` if you prefer
            return listInstance;
        }

        // Fallback => just return the List<object>
        return matchedEntities;
    }

    private async Task<IDictionary?> GetEntitiesAsync(List<TKey> ids, HttpContext context, Type dataType,
        Dictionary<string, string> metaData)
    {
        var receiver =
            serviceProvider.GetService(typeof(IBindingModelDataReceiver<,>).MakeGenericType(typeof(TKey), dataType));
        if (receiver == null)
        {
            logger.LogError($"no receiver registered for type");
            throw new InternalServerErrorException("no receiver registered for type");
        }

        var method = receiver.GetType().GetMethod(nameof(IBindingModelDataReceiver<int, int>.GetByKeys));
        if (method == null)
        {
            logger.LogError($"no receiver registered for type");
            throw new InternalServerErrorException("no receiver registered for type");
        }

        var parameters = new object?[] { ids, context, metaData };

        var invokeTask = method.Invoke(receiver, parameters);

        if (invokeTask == null)
        {
            logger.LogError($"method return null");
            throw new InternalServerErrorException("method return null");
        }

        var task = (Task)invokeTask;
        await task;

        var resultProperty = task.GetType().GetProperty(nameof(Task<int>.Result));
        if (resultProperty == null)
        {
            logger.LogError($"result property is null");
            throw new InternalServerErrorException("result property is null");
        }

        var value = resultProperty.GetValue(task);
        if (value == null)
        {
            logger.LogError($"result is null");
            throw new InternalServerErrorException("result is null");
        }

        return (IDictionary)value;
    }


    /// <summary>
    /// Internal class for storing info about a single property that needs entity binding.
    /// </summary>
    private class EntityBindingInfo
    {
        public object TargetObject { get; set; } = default!;
        public PropertyInfo TargetProperty { get; set; } = default!;
        public Type EntityType { get; set; } = default!;
        public List<TKey> Ids { get; set; } = new List<TKey>();
        public Dictionary<string, string> MetaData { get; set; } = new();
    }
}
