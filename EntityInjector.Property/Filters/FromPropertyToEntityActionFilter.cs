using System.Collections;
using System.Reflection;
using EntityInjector.Core.Interfaces;
using EntityInjector.Property.Exceptions;
using EntityInjector.Property.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace EntityInjector.Property.Filters;

public abstract class FromPropertyToEntityActionFilter<TKey>(
    IServiceProvider serviceProvider,
    ILogger<FromPropertyToEntityActionFilter<TKey>> logger)
    : IAsyncActionFilter
{
    protected abstract TKey ConvertToKey(object rawValue);

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

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.ActionArguments == null || context.ActionArguments.Count == 0)
        {
            await next();
            return;
        }

        var toProcess = EntityBindingCollector.Collect<TKey>(context.ActionArguments.Values, context.ModelState);

        var groupedByType = toProcess
            .GroupBy(info => (info.EntityType, string.Join("&", info.MetaData.OrderBy(x => x.Key).Select(x => $"{x.Key}={x.Value}"))))
            .Select(g => new { EntityType = g.Key.EntityType, MetaData = g.First().MetaData, Bindings = g.ToList() })
            .ToList();

        foreach (var group in groupedByType)
        {
            var allIds = group.Bindings.SelectMany(b => b.Ids).Distinct().ToList();
            if (allIds.Count == 0)
            {
                continue;
            }

            IDictionary? fetchedEntities;
            try
            {
                fetchedEntities = await GetEntitiesAsync(allIds, context.HttpContext, group.EntityType, group.MetaData);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error fetching entities for type {TypeName}", group.EntityType.Name);
                continue;
            }

            foreach (var binding in group.Bindings)
            {
                EntityPopulator.Populate(
                    binding.TargetObject,
                    binding.TargetProperty,
                    binding.EntityType,
                    binding.Ids.Cast<object?>().ToList(),
                    fetchedEntities,
                    context.ModelState,
                    binding.MetaData);
            }
        }

        await next();
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
}
