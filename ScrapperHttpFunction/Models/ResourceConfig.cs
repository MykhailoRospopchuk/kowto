namespace ScrapperHttpFunction.Models;

using Enums;

public record ResourceConfig
{
    public List<KeyValuePair<string, string>> Params { get; init; }
    public PathEnum Path { get; init; }

    public ResourceConfig(List<KeyValuePair<string, string>> urlParams, PathEnum path)
    {
        Params = urlParams;
        Path = path;
    }
}