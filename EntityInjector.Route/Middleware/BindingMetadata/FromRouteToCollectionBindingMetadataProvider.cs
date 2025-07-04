using EntityInjector.Route.Exceptions;
using EntityInjector.Route.Interfaces;
using EntityInjector.Route.Middleware.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace EntityInjector.Route.Middleware.BindingMetadata;

public abstract class FromRouteToCollectionBindingMetadataProvider<TKey, TValue> : IBindingMetadataProvider, IModelBinder
    where TKey : IComparable
{
    protected bool SupportsType(Type modelType)
    {
        if (modelType == typeof(TValue))
            return true;

        if (!modelType.IsGenericType) return false;
        var genericType = modelType.GetGenericTypeDefinition();
        if (genericType != typeof(List<>) && genericType != typeof(IEnumerable<>)) return false;
        var itemType = modelType.GetGenericArguments()[0];
        return itemType == typeof(TValue);

    }
    
    public void CreateBindingMetadata(BindingMetadataProviderContext context)
    {
        var attributes = context.ParameterAttributes?.OfType<FromRouteToCollectionAttribute>().ToList() ?? [];
        if (attributes.Count == 0) return;

        var targetType = context.Key.ModelType;
        if (!SupportsType(targetType))
            return;
        
        context.BindingMetadata.BindingSource = BindingSource.Custom;
        context.BindingMetadata.BinderType = GetType();
    }

    public async Task BindModelAsync(ModelBindingContext bindingContext)
    {
        if (bindingContext.ModelMetadata is not DefaultModelMetadata metadata)
        {
            throw new InternalServerErrorException($"{nameof(bindingContext.ModelMetadata)} is not {nameof(DefaultModelMetadata)}");
        }

        var attribute = metadata.Attributes.ParameterAttributes?.OfType<FromRouteToCollectionAttribute>().FirstOrDefault();
        if (attribute == null)
        {
            throw new InternalServerErrorException($"Missing {nameof(FromRouteToCollectionAttribute)} on action parameter.");
        }

        var modelType = metadata.ElementMetadata?.ModelType ?? metadata.ModelType.GetGenericArguments().First();
        var ids = GetIds(bindingContext.ActionContext, attribute.ArgumentName);

        var entities = await GetEntitiesAsync(ids, bindingContext.ActionContext, modelType, attribute.MetaData);
        bindingContext.Result = ModelBindingResult.Success(entities.Values.ToList());
    }

    private async Task<Dictionary<TKey, TValue?>> GetEntitiesAsync(List<TKey> ids, ActionContext context, Type dataType, Dictionary<string, string> metaData)
    {
        if (ids == null || ids.Count == 0)
        {
            throw new InternalServerErrorException("No IDs provided for batch resolution.");
        }

        var receiverType = typeof(IBindingModelDataReceiver<,>).MakeGenericType(typeof(TKey), dataType);
        var receiver = context.HttpContext.RequestServices.GetService(receiverType);
        if (receiver == null)
        {
            throw new InternalServerErrorException($"No receiver registered for type {receiverType.Name}");
        }

        var method = receiver.GetType().GetMethod(nameof(IBindingModelDataReceiver<TKey, TValue>.GetByKeys));
        if (method == null)
        {
            throw new InternalServerErrorException($"Method '{nameof(IBindingModelDataReceiver<TKey, TValue>.GetByKeys)}' not found on {receiver.GetType().Name}");
        }

        var parameters = new object?[] { ids, context.HttpContext, metaData };
        var taskObj = method.Invoke(receiver, parameters);

        if (taskObj is not Task task)
        {
            throw new InternalServerErrorException("Expected a Task return type from GetByKeys");
        }

        await task;

        var resultProperty = task.GetType().GetProperty("Result");
        if (resultProperty == null)
        {
            throw new InternalServerErrorException("Result property missing on resolved Task");
        }

        if (resultProperty.GetValue(task) is not Dictionary<TKey, TValue?> result)
        {
            throw new InternalServerErrorException("Result was not of expected Dictionary<TKey, TValue?> type.");
        }

        return result;
    }


    protected abstract List<TKey> GetIds(ActionContext context, string argumentName);
}
