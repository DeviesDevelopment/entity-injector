using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace EntityInjector.Samples.CosmosTest.Models;

[Table("users")]
public class User
{
    [Key] [Column("id")] [JsonProperty("id")] public Guid Id { get; set; }

    [Column("name")] [JsonProperty("name")] public string Name { get; set; } = default!;

    [Column("age")] [JsonProperty("age")] public int Age { get; set; }
}