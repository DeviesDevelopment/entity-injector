using Newtonsoft.Json;

namespace EntityInjector.Samples.CosmosTest.Models;

public class User
{
    [JsonProperty("id")] public Guid Id { get; set; }

    [JsonProperty("name")] public string Name { get; set; } = default!;

    [JsonProperty("age")] public int Age { get; set; }
}