using EntityInjector.Core.Interfaces;
using EntityInjector.Route.Attributes;
using EntityInjector.Route.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace EntityInjector.Route.BindingMetadata;

public abstract class FromRouteToCollectionBindingMetadataProvider<TKey, TValue> : IBindingMetadataProvider, IModelBinder
    where TKey : IComparable
{
    public void CreateBindingMetadata(BindingMetadataProviderContext context)
    {
        var attributes = context.ParameterAttributes?.OfType<FromRouteToCollectionAttribute>().ToList() ?? [];
        if (attributes.Count == 0) return;

        var targetType = context.Key.ModelType;
        // Skip configuration if no binding has been created for TValue
        if (!SupportsType(targetType))
            return;

        context.BindingMetadata.BindingSource = BindingSource.Custom;
        context.BindingMetadata.BinderType = GetType();
    }

    public async Task BindModelAsync(ModelBindingContext bindingContext)
    {
        if (bindingContext.ModelMetadata is not DefaultModelMetadata metadata)
            throw new UnexpectedBindingResultException(typeof(DefaultModelMetadata), bindingContext.ModelMetadata?.GetType());

        var attribute = metadata.Attributes.ParameterAttributes?.OfType<FromRouteToCollectionAttribute>()
            .FirstOrDefault();
        if (attribute == null)
            throw new MissingRouteAttributeException(bindingContext.FieldName ?? "<unknown>", nameof(FromRouteToCollectionAttribute));

        var modelType = metadata.ElementMetadata?.ModelType ?? metadata.ModelType.GetGenericArguments().First();
        var ids = GetIds(bindingContext.ActionContext, attribute.ArgumentName);

        var entities = await GetEntitiesAsync(ids, bindingContext.ActionContext, modelType, attribute.MetaData);
        bindingContext.Result = ModelBindingResult.Success(entities.Values.ToList());
    }

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

    private async Task<Dictionary<TKey, TValue?>> GetEntitiesAsync(List<TKey> ids, ActionContext context, Type dataType,
        Dictionary<string, string> metaData)
    {
        if (ids == null || ids.Count == 0)
            throw new UnexpectedBindingResultException(typeof(List<TKey>), null);

        var receiverType = typeof(IBindingModelDataReceiver<,>).MakeGenericType(typeof(TKey), dataType);
        var receiver = context.HttpContext.RequestServices.GetService(receiverType);
        if (receiver == null)
            throw new BindingReceiverNotRegisteredException(receiverType);

        var method = receiver.GetType().GetMethod(nameof(IBindingModelDataReceiver<TKey, TValue>.GetByKeys));
        if (method == null)
            throw new BindingReceiverContractException(nameof(IBindingModelDataReceiver<TKey, TValue>.GetByKeys), receiver.GetType());

        var parameters = new object?[] { ids, context.HttpContext, metaData };
        var taskObj = method.Invoke(receiver, parameters);

        if (taskObj is not Task task)
            throw new UnexpectedBindingResultException(typeof(Task), taskObj?.GetType());

        await task;

        var resultProperty = task.GetType().GetProperty("Result");
        if (resultProperty == null)
            throw new UnexpectedBindingResultException(typeof(Dictionary<TKey, TValue?>), null);

        var value = resultProperty.GetValue(task);
        if (value is not Dictionary<TKey, TValue?> result)
            throw new UnexpectedBindingResultException(typeof(Dictionary<TKey, TValue?>), value?.GetType());

        return result;
    }

    protected abstract List<TKey> GetIds(ActionContext context, string argumentName);
}
