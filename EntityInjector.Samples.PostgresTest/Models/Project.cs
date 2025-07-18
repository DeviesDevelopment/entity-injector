using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EntityInjector.Property.Attributes;
using EntityInjector.Samples.PostgresTest.Models.Entities;

namespace EntityInjector.Samples.PostgresTest.Models;

public class ProjectModel
{
    public Guid Id { get; set; }
    
    public string Name { get; set; } = "";

    public List<Guid> LeadIds { get; set; } = [];

    [FromPropertyToEntity(nameof(LeadIds))]
    public List<User> Leads { get; set; } = [];
}

public class ProjectModelWithNullableLeads
{
    public Guid Id { get; set; }

    public string Name { get; set; } = "";

    public List<Guid?> LeadIds { get; set; } = [];

    [FromPropertyToEntity(nameof(LeadIds), "includeNulls=true")]
    public List<User?> Leads { get; set; } = [];
}

public class ProjectDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public List<User?> Leads { get; set; } = [];
}
