using Microsoft.AspNetCore.Http;

namespace EntityInjector.Route.Exceptions;

public abstract class RouteBindingException(string message, Exception? inner = null)
    : Exception(message, inner)
{
    public abstract int StatusCode { get; }
}

public sealed class RouteEntityNotFoundException(string entityName, object? id)
    : RouteBindingException($"No {entityName} found for ID '{id}'.")
{
    public override int StatusCode => StatusCodes.Status404NotFound;

    public string EntityName { get; } = entityName;
    public object? Id { get; } = id;
}

public sealed class MissingRouteAttributeException(string parameterName, string expectedAttribute)
    : RouteBindingException($"Missing required {expectedAttribute} on action parameter '{parameterName}'.")
{
    public override int StatusCode => StatusCodes.Status400BadRequest;
}

public sealed class UnsupportedBindingTypeException(Type targetType)
    : RouteBindingException($"The type '{targetType.Name}' is not supported for route binding.")
{
    public override int StatusCode => StatusCodes.Status400BadRequest;

    public Type TargetType { get; } = targetType;
}

public sealed class BindingReceiverNotRegisteredException(Type receiverType)
    : RouteBindingException($"No binding receiver registered for type '{receiverType.FullName}'.")
{
    public override int StatusCode => StatusCodes.Status500InternalServerError;

    public Type ReceiverType { get; } = receiverType;
}

public sealed class BindingReceiverContractException(string methodName, Type receiverType)
    : RouteBindingException($"Expected method '{methodName}' not found on receiver type '{receiverType.Name}'.")
{
    public override int StatusCode => StatusCodes.Status500InternalServerError;

    public string MethodName { get; } = methodName;
    public Type ReceiverType { get; } = receiverType;
}

public sealed class UnexpectedBindingResultException(Type expected, Type? actual)
    : RouteBindingException($"Expected result of type '{expected.Name}', but got '{actual?.Name ?? "null"}'.")
{
    public override int StatusCode => StatusCodes.Status500InternalServerError;

    public Type ExpectedType { get; } = expected;
    public Type? ActualType { get; } = actual;
}

public sealed class MissingRouteParameterException(string parameterName)
    : RouteBindingException($"Route parameter '{parameterName}' was not found. Ensure it is correctly specified in the route.")
{
    public override int StatusCode => StatusCodes.Status400BadRequest;

    public string ParameterName { get; } = parameterName;
}

public sealed class InvalidRouteParameterFormatException(string parameterName, Type expectedType, Type actualType)
    : RouteBindingException($"Route parameter '{parameterName}' is of type '{actualType.Name}', but type '{expectedType.Name}' was expected.")
{
    public override int StatusCode => StatusCodes.Status422UnprocessableEntity;

    public string ParameterName { get; } = parameterName;
    public Type ExpectedType { get; } = expectedType;
    public Type ActualType { get; } = actualType;
}

public sealed class EmptyRouteSegmentListException(string parameterName)
    : RouteBindingException($"Route parameter '{parameterName}' did not contain any valid string segments.")
{
    public override int StatusCode => StatusCodes.Status422UnprocessableEntity;

    public string ParameterName { get; } = parameterName;
}
