namespace ScrapperHttpFunction.CosmoDatabase.Entities;

using Base;
using Newtonsoft.Json;

public class RequestsId : IKeyEntity, IOutType
{
    [JsonProperty(PropertyName = "id")]
    public string Id { get; set; }
}