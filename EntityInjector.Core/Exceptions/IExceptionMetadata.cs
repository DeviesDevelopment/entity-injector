namespace EntityInjector.Core.Exceptions;

public interface IExceptionMetadata
{
    int StatusCode { get; }
    string DefaultDescription { get; }
}
