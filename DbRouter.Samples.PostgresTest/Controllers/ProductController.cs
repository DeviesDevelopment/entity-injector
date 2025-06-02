using DbRouter.Middleware.Attributes;
using DbRouter.Samples.PostgresTest.Models;
using Microsoft.AspNetCore.Mvc;

namespace DbRouter.Samples.PostgresTest.Controllers;

[ApiController]
[Route("api/products")]
public class ProductController : ControllerBase
{
    [HttpGet("{id:int}")]
    public ActionResult<Product> GetOne([FromRouteToEntity("id")] Product product)
    {
        return Ok(new { product.Id, product.Name, product.Price });
    }
    
    [HttpGet("batch/{ids}")]
    public ActionResult<IEnumerable<Product>> GetMany([FromRouteToCollection("ids")] List<Product> products)
    {
        return Ok(products.Select(p => new { p.Id, p.Name, p.Price }));
    }
}
