using EntityInjector.Core.Interfaces;
using EntityInjector.Route.Attributes;
using EntityInjector.Route.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace EntityInjector.Route.BindingMetadata;

public abstract class FromRouteToEntityBindingMetadataProvider<TKey, TValue> : IBindingMetadataProvider, IModelBinder
    where TKey : IComparable
{
    public void CreateBindingMetadata(BindingMetadataProviderContext context)
    {
        var fromRouteParameterAttributes =
            context.ParameterAttributes?.OfType<FromRouteToEntityAttribute>().ToList() ?? [];

        if (fromRouteParameterAttributes.Count == 0) return;

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

        var attribute = metadata.Attributes.ParameterAttributes?.OfType<FromRouteToEntityAttribute>().FirstOrDefault();
        if (attribute == null)
            throw new MissingRouteAttributeException(bindingContext.FieldName ?? "unknown", nameof(FromRouteToEntityAttribute));

        var id = GetId(bindingContext.ActionContext, attribute.ArgumentName);
        var dataType = metadata.ModelType;

        var entity = await GetEntityAsync(id, bindingContext.ActionContext, dataType, attribute.MetaData);
        if (entity is null)
            throw new RouteEntityNotFoundException(dataType.Name, id);

        bindingContext.Result = ModelBindingResult.Success(entity);
    }

    protected bool SupportsType(Type modelType)
    {
        return modelType == typeof(TValue);
    }

    protected abstract TKey GetId(ActionContext context, string argumentName);

    private async Task<TValue?> GetEntityAsync(TKey id, ActionContext context, Type dataType,
        Dictionary<string, string> metaData)
    {
        var receiverType = typeof(IBindingModelDataReceiver<,>).MakeGenericType(typeof(TKey), dataType);
        var receiver = context.HttpContext.RequestServices.GetService(receiverType);
        if (receiver == null)
            throw new BindingReceiverNotRegisteredException(receiverType);

        var method = receiver.GetType().GetMethod(nameof(IBindingModelDataReceiver<TKey, TValue>.GetByKey));
        if (method == null)
            throw new BindingReceiverContractException(nameof(IBindingModelDataReceiver<TKey, TValue>.GetByKey), receiver.GetType());

        var parameters = new object?[] { id, context.HttpContext, metaData };
        var taskObj = method.Invoke(receiver, parameters);

        if (taskObj is not Task task)
            throw new UnexpectedBindingResultException(typeof(Task), taskObj?.GetType());

        await task;

        var resultProperty = task.GetType().GetProperty("Result");
        if (resultProperty == null)
            throw new UnexpectedBindingResultException(typeof(object), null);

        var result = resultProperty.GetValue(task);

        if (result is null)
            return default;
        
        if (result is not TValue typedResult)
            throw new UnexpectedBindingResultException(typeof(TValue), result?.GetType());

        return typedResult;
    }
}
