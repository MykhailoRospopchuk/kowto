namespace ScrapperHttpFunction.FunctionRequestDTO;

using RequestModels;

public sealed class ResourceRegistrationRequestDTO
{
    public string[] DeleteRecordIds { get; set; }
    public List<ResourceModel> Resources { get; set; } = new();
}