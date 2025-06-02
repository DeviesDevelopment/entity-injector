using DbRouter.Middleware.Attributes;
using DbRouter.Samples.PostgresTest.Models;
using Microsoft.AspNetCore.Mvc;

namespace DbRouter.Samples.PostgresTest.Controllers;

[ApiController]
[Route("api/users")]
public class UserController : ControllerBase
{
    [HttpGet("{id:guid}")]
    public ActionResult<User> GetOne([FromRouteToEntity("id")] User user)
    {
        return Ok(new { user.Id, user.Name, user.Age });
    }
    
    [HttpGet("batch/{ids}")]
    public ActionResult<IEnumerable<User>> GetMany([FromRouteToCollection("ids")] List<User> users)
    {
        return Ok(users.Select(u => new { u.Id, u.Name, u.Age }));
    }
}
