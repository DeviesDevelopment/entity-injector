using EntityInjector.Route.Exceptions;
using EntityInjector.Route.Interfaces;
using EntityInjector.Route.Middleware.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace EntityInjector.Route.Middleware;

public abstract class FromRouteToEntityBindingMetadataProvider<TKey, TValue> : IBindingMetadataProvider, IModelBinder where TKey : IComparable
{
    protected bool SupportsType(Type modelType) => modelType == typeof(TValue);
    
    public void CreateBindingMetadata(BindingMetadataProviderContext context)
    {
        var fromRouteParameterAttributes =
            context.ParameterAttributes?.OfType<FromRouteToEntityAttribute>().ToList() ?? [];

        if (fromRouteParameterAttributes.Count == 0) return;

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

        var attribute = metadata.Attributes.ParameterAttributes?.OfType<FromRouteToEntityAttribute>().FirstOrDefault();
        if (attribute == null)
        {
            throw new InternalServerErrorException($"Missing {nameof(FromRouteToEntityAttribute)} on action parameter.");
        }

        var id = GetId(bindingContext.ActionContext, attribute.ArgumentName);
        var dataType = metadata.ModelType;

        var entity = await GetEntityAsync(id, bindingContext.ActionContext, dataType, attribute.MetaData);
        if (entity == null)
        {
            throw new NotFoundException($"Route value '{attribute.ArgumentName}' - No {dataType.Name} with Id: {id}");
        }

        bindingContext.Result = ModelBindingResult.Success(entity);
    }

    protected abstract TKey GetId(ActionContext context, string argumentName);

    private async Task<TValue?> GetEntityAsync(TKey id, ActionContext context, Type dataType, Dictionary<string, string> metaData)
    {
        var receiverType = typeof(IBindingModelDataReceiver<,>).MakeGenericType(typeof(TKey), dataType);
        var receiver = context.HttpContext.RequestServices.GetService(receiverType);
        if (receiver == null)
        {
            throw new InternalServerErrorException($"No receiver registered for type {receiverType.Name}");
        }

        var method = receiver.GetType().GetMethod(nameof(IBindingModelDataReceiver<TKey, TValue>.GetByKey));
        if (method == null)
        {
            throw new InternalServerErrorException($"Method '{nameof(IBindingModelDataReceiver<TKey, TValue>.GetByKey)}' not found on {receiver.GetType().Name}");
        }

        var parameters = new object?[] { id, context.HttpContext, metaData };
        var taskObj = method.Invoke(receiver, parameters);

        if (taskObj is not Task task)
        {
            throw new InternalServerErrorException("Expected a Task return type from GetByKey");
        }

        await task;

        var resultProperty = task.GetType().GetProperty("Result");
        if (resultProperty == null)
        {
            throw new InternalServerErrorException("Result property missing on resolved Task");
        }

        var result = resultProperty.GetValue(task);

        if (result is not TValue typedResult)
        {
            throw new InternalServerErrorException($"Expected result of type {typeof(TValue).Name}, but got {result?.GetType().Name ?? "null"}");
        }

        return typedResult;
    }
}
