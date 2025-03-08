namespace ScrapperHttpFunction.Models.DatabaseModels;

using CosmoDatabase.Base;
using Newtonsoft.Json;

public class JobInfoOutModel : IOutType
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

    [JsonProperty(PropertyName = "_ts")]
    public long TimestampUnix { get; set; }
}