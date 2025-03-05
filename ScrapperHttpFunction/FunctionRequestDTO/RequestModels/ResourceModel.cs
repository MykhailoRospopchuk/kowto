namespace ScrapperHttpFunction.FunctionRequestDTO.RequestModels;

using Enums;

public class ResourceModel
{
    public PathEnum Path { get; set; }
    public List<KeyValuePair<string, string>> Params { get; set; } = new ();
}