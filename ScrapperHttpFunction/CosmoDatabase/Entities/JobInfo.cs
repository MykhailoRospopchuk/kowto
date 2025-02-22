namespace ScrapperHttpFunction.CosmoDatabase.Entities;

using System.Text.Json.Serialization;

public class JobInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
    
    [JsonPropertyName("date")]
    public string Date { get; set; }
    
    [JsonPropertyName("title")]
    public string Title { get; set; }
    
    [JsonPropertyName("url")]
    public string Url { get; set; }
    
    [JsonPropertyName("company")]
    public string CompanyName { get; set; }
}