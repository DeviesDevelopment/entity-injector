using EntityInjector.Route.Middleware.Attributes;
using EntityInjector.Samples.PostgresTest.Models;
using Microsoft.AspNetCore.Mvc;

namespace EntityInjector.Samples.PostgresTest.Controllers;

[ApiController]
[Route("api/invalid/users")]
public class InvalidUserController : ControllerBase
{
    // Used to ensure correct status code when invalid parameter is inserted into id
    [HttpGet("{id?}")]
    public ActionResult<User> GetMaybe([FromRouteToEntity("id")] User user)
    {
        return Ok(new { user.Id, user.Name, user.Age });
    }
    
    // Used to ensure correct status code when invalid parameter is inserted into id
    [HttpGet("batch/{ids?}")]
    public ActionResult<IEnumerable<User>> GetManyMaybe([FromRouteToCollection("ids")] List<User> users)
    {
        return Ok(users.Select(u => new { u.Id, u.Name, u.Age }));
    }
}