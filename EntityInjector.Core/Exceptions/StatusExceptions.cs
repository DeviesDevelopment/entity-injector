using Microsoft.AspNetCore.Http;

namespace EntityInjector.Core.Exceptions;

public sealed class EntityNotFoundException(string entityName, object? id)
    : EntityBindingException($"No {entityName} found for ID '{id}'."), IExceptionMetadata
{
    public override int StatusCode => StatusCodes.Status404NotFound;
    public string DefaultDescription => "The requested entity was not found.";
    public string EntityName { get; } = entityName;
    public object? Id { get; } = id;
}

public sealed class MissingEntityAttributeException(string parameterName, string expectedAttribute)
    : EntityBindingException($"Missing required {expectedAttribute} on action parameter '{parameterName}'."), IExceptionMetadata
{
    public override int StatusCode => StatusCodes.Status400BadRequest;
    public string DefaultDescription => "A required parameter attribute was missing.";
}

public sealed class UnsupportedBindingTypeException(Type targetType)
    : EntityBindingException($"The type '{targetType.Name}' is not supported for route binding.")
{
    public override int StatusCode => StatusCodes.Status400BadRequest;

    public Type TargetType { get; } = targetType;
}

public sealed class BindingReceiverNotRegisteredException(Type receiverType)
    : EntityBindingException($"No binding receiver registered for type '{receiverType.FullName}'.")
{
    public override int StatusCode => StatusCodes.Status500InternalServerError;

    public Type ReceiverType { get; } = receiverType;
}

public sealed class BindingReceiverContractException(string methodName, Type receiverType)
    : EntityBindingException($"Expected method '{methodName}' not found on receiver type '{receiverType.Name}'.")
{
    public override int StatusCode => StatusCodes.Status500InternalServerError;

    public string MethodName { get; } = methodName;
    public Type ReceiverType { get; } = receiverType;
}

public sealed class UnexpectedBindingResultException(Type expected, Type? actual)
    : EntityBindingException($"Expected result of type '{expected.Name}', but got '{actual?.Name ?? "null"}'.")
{
    public override int StatusCode => StatusCodes.Status500InternalServerError;

    public Type ExpectedType { get; } = expected;
    public Type? ActualType { get; } = actual;
}

public sealed class MissingEntityParameterException(string parameterName)
    : EntityBindingException($"Route parameter '{parameterName}' was not found. Ensure it is correctly specified in the route."), IExceptionMetadata
{
    public override int StatusCode => StatusCodes.Status400BadRequest;
    public string DefaultDescription => "A required route parameter was missing.";
    public string ParameterName { get; } = parameterName;
}

public sealed class InvalidEntityParameterFormatException(string parameterName, Type expectedType, Type actualType)
    : EntityBindingException($"Route parameter '{parameterName}' is of type '{actualType.Name}', but type '{expectedType.Name}' was expected."), IExceptionMetadata
{
    public override int StatusCode => StatusCodes.Status422UnprocessableEntity;
    public string DefaultDescription => "A route parameter was not in the expected format.";
    public string ParameterName { get; } = parameterName;
    public Type ExpectedType { get; } = expectedType;
    public Type ActualType { get; } = actualType;
}

public sealed class EmptyEntitySegmentListException(string parameterName)
    : EntityBindingException($"Route parameter '{parameterName}' did not contain any valid string segments."), IExceptionMetadata
{
    public override int StatusCode => StatusCodes.Status422UnprocessableEntity;
    public string DefaultDescription => "The route parameter did not contain any valid values.";
    public string ParameterName { get; } = parameterName;
}
