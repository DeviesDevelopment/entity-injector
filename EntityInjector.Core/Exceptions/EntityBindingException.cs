namespace EntityInjector.Core.Exceptions;

public abstract class EntityBindingException(string message, Exception? inner = null)
    : Exception(message, inner)
{
    public abstract int StatusCode { get; }
    public virtual string? Description => Message;
}