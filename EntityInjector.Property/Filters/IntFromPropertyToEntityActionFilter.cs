using EntityInjector.Property.Exceptions;
using Microsoft.Extensions.Logging;

namespace EntityInjector.Property.Filters;

public class IntFromPropertyToEntityActionFilter(
    IServiceProvider serviceProvider,
    ILogger<IntFromPropertyToEntityActionFilter> logger)
    : FromPropertyToEntityActionFilter<int>(serviceProvider, logger)
{
 
    protected override int ConvertToKey(object rawValue) => rawValue switch
    {
        int i => i,
        long l and >= int.MinValue and <= int.MaxValue => (int)l,
        short s => s,
        byte b => b,
        string str when int.TryParse(str, out var parsed) => parsed,
        double d when d % 1 == 0 && d is >= int.MinValue and <= int.MaxValue => (int)d,
        float f when f % 1 == 0 && f is >= int.MinValue and <= int.MaxValue => (int)f,
        decimal m when m % 1 == 0 && m is >= int.MinValue and <= int.MaxValue => (int)m,
        _ => throw new InternalServerErrorException($"Cannot convert '{rawValue}' ({rawValue.GetType().Name}) to int")
    };


    protected override int GetDefaultValueForNull() => 0;
}
