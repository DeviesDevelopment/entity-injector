using Newtonsoft.Json;

namespace EntityInjector.Samples.CosmosTest.Models;

public class Product
{
    [JsonProperty("id")] public string Id { get; set; } = default!; // Id may not be int in cosmos

    [JsonProperty("name")] public string Name { get; set; } = "";

    [JsonProperty("price")] public decimal Price { get; set; }
}