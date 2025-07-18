using EntityInjector.Samples.PostgresTest.Models;
using Microsoft.AspNetCore.Mvc;

namespace EntityInjector.Samples.PostgresTest.Controllers;

[ApiController]
[Route("api/projects")]
public class ProjectController : ControllerBase
{
    [HttpPost]
    public ActionResult<ProjectDto> FakeCreateProject([FromBody] ProjectModel model)
    {
        return Ok(new ProjectDto
        {
            Id = model.Id,
            Name = model.Name,
            Leads = model.Leads!
        });
    }
    
    [HttpPost("nullable")]
    public ActionResult<ProjectDto> PostProjectWithNullableLeads([FromBody] ProjectModelWithNullableLeads model)
    {
        return Ok(new ProjectDto
        {
            Id = model.Id,
            Name = model.Name,
            Leads = model.Leads
        });
    }
}