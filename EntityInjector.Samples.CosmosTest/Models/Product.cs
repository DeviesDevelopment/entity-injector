using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace EntityInjector.Samples.CosmosTest.Models;

public class Product
{
    [Key]
    [Column("id")]
    [JsonProperty("id")]
    public string Id { get; set; } = default!; // Id may not be int in cosmos

    [Column("name")] [JsonProperty("name")] public string Name { get; set; } = "";

    [Column("price")] [JsonProperty("price")] public decimal Price { get; set; }
    
}