# EntityInjector

The purpose of this package is to simplify the process of extracting entities from (primarily) databases.

## Usage

Many controllers implement methods to fetch database models, this package makes that process easier. It works via
dependency injection, which avoids direct dependencies to database repositories in for example your controller classes.

The usage is not explicitly restricted to controllers, but that is where this behaviour is most common, and it uses the
HTTP context to determine the argument.

```c#
[HttpGet("{id:guid}")]
public ActionResult<User> GetOne([FromRouteToEntity("id")] User user)
{
    return Ok(new { user.Id, user.Name, user.Age });
}
```

## Set up

To set up your project, install this package and do the following operations:

- Add a DataReceiver which implements the interface `IBindingModelDataReceiver<Guid, User>` to add the fetching logic
  dependent on your database.
- Add dependency injection for your DataReceiver (
  `services.AddScoped<IBindingModelDataReceiver<Guid, User>, GuidUserDataReceiver>();`)
- Add dependency injection for the specific `BindingMetadataProviders` you want (
  `options.ModelMetadataDetailsProviders.Add(new GuidEntityBindingMetadataProvider<User>());`)

## Samples

There are examples with a postgres (EntityFramework + TestContainers) database which implements this for 2 simple
models. It shows how you can fetch both one and multiple entities at the same time, and how to configure them with
differing database keys.

## Extensibility

There are currently a limited amount of implementations of the `BindingMetadataProviders`,
but there is nothing stopping you from extending `FromRouteToEntityBindingMetadataProvider` or
`FromRouteToCollectionBindingMetadataProvider` to use another key.

## Limitations

There is a limit that for each data type, there can only be one key used. This is due to ambiguous type resolution.

If you really want multiple ways to reach the same datatype. It is possible to extend FromRouteToEntityAttribute and
give it a different name that can be used simultaneously. However, that behaviour is discouraged.

## Tests

The tests can be seen in each respective sample. Their databases are set up via simple docker containers. Instructions
for how to run them exists in the respective sample

## TODO:

Make repository public

Add sample for cosmos db

Add pipeline for running the tests

Extend the tests (with for example expecting failure)

Add exception handling