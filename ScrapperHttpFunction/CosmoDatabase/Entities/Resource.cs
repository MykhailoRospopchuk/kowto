namespace ScrapperHttpFunction.CosmoDatabase.Entities;

using Enums;
using Newtonsoft.Json;

public class Resource : IKeyEntity
{
    [JsonProperty(PropertyName = "id")]
    public string Id { get; set; }
    
    [JsonProperty(PropertyName = "path")]
    public PathEnum Path { get; set; }
    
    [JsonProperty(PropertyName = "params")]
    public List<KeyValuePair<string, string>> Params { get; set; } = new ();
}