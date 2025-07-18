using EntityInjector.Property.Attributes;
using EntityInjector.Samples.PostgresTest.Models.Entities;

namespace EntityInjector.Samples.PostgresTest.Models;

public class PetModel
{
    public Guid Id { get; set; }

    public string Name { get; set; } = "";

    public string Species { get; set; } = "";

    public Guid OwnerId { get; set; }

    [FromPropertyToEntity(nameof(OwnerId))]
    public User? Owner { get; set; }
}

public class PetModelWithNullableOwner
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Species { get; set; } = "";

    public Guid? OwnerId { get; set; }

    [FromPropertyToEntity(nameof(OwnerId))]
    public User? Owner { get; set; }
}

public class PetDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Species { get; set; } = "";
    public User? Owner { get; set; }
}