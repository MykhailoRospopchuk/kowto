namespace ScrapperHttpFunction.Models;

using Enums;

public record ResourceConfig
{
    public List<KeyValuePair<string, string>> Params { get; init; } = new ();
    public PathEnum Path { get; init; }

    public ResourceConfig(PathEnum path, List<KeyValuePair<string, string>> urlParams)
    {
        Params = urlParams;
        Path = path;
    }
}