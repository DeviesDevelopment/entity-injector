namespace EntityInjector.Core.Exceptions;

public abstract class RouteBindingException(string message, Exception? inner = null)
    : Exception(message, inner)
{
    public abstract int StatusCode { get; }
}