using EntityInjector.Samples.PostgresTest.Models;
using EntityInjector.Samples.PostgresTest.Models.Entities;
using Microsoft.AspNetCore.Mvc;

namespace EntityInjector.Samples.PostgresTest.Controllers;

[ApiController]
[Route("api/pets")]
public class PetController : ControllerBase
{
    [HttpPost]
    public ActionResult<PetDto> FakeCreate([FromBody] PetModel pet)
    {
        return Ok(new PetDto
        {
            Id = pet.Id,
            Name = pet.Name,
            Species = pet.Species,
            Owner = pet.Owner is null
                ? null
                : new User
                {
                    Id = pet.Owner.Id,
                    Name = pet.Owner.Name,
                    Age = pet.Owner.Age
                }
        });
    }

    [HttpPost("bulk")]
    public ActionResult<List<PetDto>> FakeCreateBulk([FromBody] List<PetModel> pets)
    {
        return pets.Select(p => new PetDto
        {
            Id = p.Id,
            Name = p.Name,
            Species = p.Species,
            Owner = p.Owner
        }).ToList();
    }

    [HttpPost("by-name")]
    public ActionResult<Dictionary<string, PetDto>> FakeCreateByName([FromBody] Dictionary<string, PetModel> petsByName)
    {
        var result = petsByName.ToDictionary(
            kvp => kvp.Key,
            kvp => new PetDto
            {
                Id = kvp.Value.Id,
                Name = kvp.Value.Name,
                Species = kvp.Value.Species,
                Owner = kvp.Value.Owner
            });

        return Ok(result);
    }

    [HttpPost("nullable")]
    public ActionResult<PetDto> PostNullableOwner([FromBody] PetModelWithNullableOwner model)
    {
        return Ok(new PetDto
        {
            Id = model.Id,
            Name = model.Name,
            Species = model.Species,
            Owner = model.Owner
        });
    }
}