using Microsoft.Azure.Cosmos;

namespace EntityInjector.Samples.CosmosTest.Setup;

public class CosmosContainer<T>(Container container)
{
    public Container Container { get; } = container;
}