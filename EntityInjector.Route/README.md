# EntityInjector.Route

EntityInjector.Route simplifies binding route parameters to entities using dependency injection.  
It is designed primarily for controllers but can be used anywhere in the pipeline where route data is available.

## Usage

```csharp
[HttpGet("{id:guid}")]
public ActionResult<User> GetOne([FromRouteToEntity("id")] User user)
{
    return Ok(new { user.Id, user.Name, user.Age });
}
```

## Setup

1. Register a `DataReceiver` that implements `IBindingModelDataReceiver<Guid, User>`  
   This is where you define how to fetch entities from your data source.

```csharp
services.AddScoped<IBindingModelDataReceiver<Guid, User>, GuidUserDataReceiver>();
```

2. Register the model metadata provider:

```csharp
options.ModelMetadataDetailsProviders.Add(new GuidEntityBindingMetadataProvider<User>());
```

## Samples

See the Postgres sample using Entity Framework Core and TestContainers:  
https://github.com/devies-ab/EntityInjector.Route/tree/main/EntityInjector.Samples.PostgresTest

The sample demonstrates:

- Fetching single and multiple entities
- Configuring different entity keys

## Extensibility

You can extend `FromRouteToEntityBindingMetadataProvider` or `FromRouteToCollectionBindingMetadataProvider` to support custom key types beyond what is included.

## Limitations

Only one key type is supported per entity type to avoid ambiguity during binding.  
If multiple keys are needed, you can create a custom attribute extending `FromRouteToEntityAttribute` with a different name.  
This is technically possible but not recommended due to potential for confusion.
