namespace EntityInjector.Property.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }
}

public class InternalServerErrorException : Exception
{
    public InternalServerErrorException(string message) : base(message)
    {
    }
}

public class BadConditionException : Exception
{
    public BadConditionException(string message) : base(message)
    {
    }
}