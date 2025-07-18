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

3. (Optionally) Configure exception handling:

```csharp
services.AddRouteBinding();
...
app.UseRouteBinding();
```

You may also opt to configure your own `ProblemDetailsFactory` to customize your exception logic (to for example hide
error messages).
In such a case avoid `app.UseRouteBinding()` and instead add your own:

```csharp
services.TryAddSingleton<IRouteBindingProblemDetailsFactory, YourCustomRouteBindingProblemDetailsFactory>();
```

An example of this can be found in the `CustomFactoryExceptionTests`

4. (Optionally) Add a swagger filter for the entities:

```csharp
services.PostConfigureAll<SwaggerGenOptions>(o =>
{
    o.OperationFilter<FromRouteToEntityOperationFilter>();
});
```

## Samples

See the Sample projects for demonstration on how to:

- Configure a Postgres database with TestContainers
- Configure a Cosmos database
- Fetching single and multiple entities
- Configuring different entity keys
- Enabling exception management
- Configuring custom exception management

## Extensibility

You can extend `FromRouteToEntityBindingMetadataProvider` or `FromRouteToCollectionBindingMetadataProvider` to support
custom key types beyond what is included.

You can also configure your own exception management as described earlier.

## Limitations

Only one key type is supported per entity type to avoid ambiguity during binding.  
If multiple keys are needed, you can create a custom attribute extending `FromRouteToEntityAttribute` with a different
name.  
This is technically possible but not recommended due to potential for confusion.
