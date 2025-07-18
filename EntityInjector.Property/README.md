# EntityInjector.Property

EntityInjector.Property simplifies resolving related entities from model properties using [FromPropertyToEntity].
It is designed for enriching incoming request models (e.g., DTOs in POST/PUT) based on identifier properties, enabling clean validation and service logic.
Usage
```csharp
public class PetModel
{
    public Guid OwnerId { get; set; }

    [FromPropertyToEntity(nameof(OwnerId))]
    public User? Owner { get; set; }
}

[HttpPost]
public IActionResult Create([FromBody] PetModel model)
{
    // Owner is already resolved and validated
    _service.Register(model.Owner, model);
    return Ok();
}
```

## Setup

1. Register a DataReceiver that implements `IBindingModelDataReceiver<TKey, TEntity>`. This defines how to fetch entities based on key properties.
```csharp
services.AddScoped<IBindingModelDataReceiver<Guid, User>, GuidUserDataReceiver>();
```

2. Register the corresponding action filter for the key type used (e.g., `Guid`, `string`,`int`):
```csharp
options.Filters.Add<GuidFromPropertyToEntityActionFilter>();
```

Each key type requires a separate action filter inheriting from FromPropertyToEntityActionFilter<TKey>.

3. (Optional) Configure custom exception behavior. The default implementation throws a RouteBindingException if an entity cannot be resolved. You can override this by customizing your data receiver or extending the exception pipeline.

## Attribute Behavior

You can control how missing or unmatched entity references are handled using optional metadata flags in the `[FromPropertyToEntity]` attribute:

### cleanNoMatch

Type: `bool` (as a `string` key in MetaData)

Purpose: Suppresses model validation errors when a referenced ID does not resolve to an entity.

Default behavior (when omitted): A model error is added if an ID has no matching entity.

Usage example:
```csharp
[FromPropertyToEntity(nameof(LeadIds), "cleanNoMatch=true")]
public List<User?> Users { get; set; }
```

### includeNulls

Type: `bool` (as a `string` key in MetaData)

Purpose: Ensures that for any unmatched or null ID, a null is explicitly added to the target entity, collection or dictionary.

Important:

If both `includeNulls=true` and `cleanNoMatch=true` are specified, includeNulls takes precedence.

This means that null values will be inserted for unmatched IDs, and no model errors will be added, regardless of cleanNoMatch.

Usage example:
```csharp
[FromPropertyToEntity(nameof(LeadIds), "cleanNoMatch=true", "includeNulls=true")]
public List<User?> Users { get; set; }
```

## Use Cases for `[FromPropertyToEntity]`

The `[FromPropertyToEntity]` attribute enables automatic resolution of related entities from foreign key values in incoming models. This helps simplify controller logic and improve validation, especially in write operations.

### Entity Validation on Write

Ensure referenced entities (e.g., User, Product, Organization) exist before performing operations like `POST`, `PUT`, or `PATCH`.
```csharp
{ "ownerId": "abc123", "name": "Bella" }
```

With `[FromPropertyToEntity(nameof(OwnerId))]`, you can fail early if the referenced user doesn’t exist.

### Flattened Input Models (Frontend-Friendly)

Instead of requiring full nested objects, clients can send flat models with only IDs:
```csharp
{ "productId": 42, "quantity": 3 }
```

Your backend gets the resolved Product object ready-to-use.

### Authorization Checks

Easily check whether the current user has access to the resolved entity:
```csharp
if (!UserCanAccess(model.Customer)) return Forbid();
```

No manual repository calls are needed — the entity is already injected.

### Batch Entity Resolution

Handle lists of entities efficiently by grouping and resolving foreign keys in bulk — one query per entity type, regardless of how many items.
```csharp
[
  { "name": "Bella", "ownerId": "abc123" },
  { "name": "Max", "ownerId": "abc123" }
]
```

The owner is only fetched once, improving performance.

### Recursive Enrichment of Nested Structures

Supports nested models with entity references:
```csharp
public class ProjectModel {
    public List<TeamMemberModel> TeamMembers { get; set; }
}

public class TeamMemberModel {
    public string UserId { get; set; }

    [FromPropertyToEntity(nameof(UserId))]
    public User? User { get; set; }
}
```

Each TeamMember gets its corresponding User injected.

### Simplified Service Layer Usage

Because the full entities are populated on the DTO, service layers can work directly with them:
```csharp
_petService.RegisterNewPet(dto.Owner, dto.Name, dto.Species);
```

No need to manually resolve entities from IDs inside the service.

### Enhanced Logging and Auditing

Capture meaningful details (e.g., resolved User.Name) in logs without extra queries.

## Samples

See the sample project for demonstrations on:

* Mapping identifiers to entity references in POST bodies

* Combining `[FromPropertyToEntity]` with other injection or validation patterns

* Using different key types (`Guid`, `string`, `int`) with dedicated filters

* Testing injection logic using TestServer
