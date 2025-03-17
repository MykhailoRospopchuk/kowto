namespace ScrapperHttpFunction.CosmoDatabase.Entities;

using Base;
using Newtonsoft.Json;

public class JobInfo : IKeyEntity, IOutType
{
    [JsonProperty(PropertyName = "id")]
    public string Id { get; set; }

    [JsonProperty(PropertyName = "date")]
    public string Date { get; set; }

    [JsonProperty(PropertyName = "title")]
    public string Title { get; set; }

    [JsonProperty(PropertyName = "url")]
    public string Url { get; set; }

    [JsonProperty(PropertyName = "company")]
    public string CompanyName { get; set; }

    [JsonProperty(PropertyName = "hash")]
    public string Hash { get; set; }
}