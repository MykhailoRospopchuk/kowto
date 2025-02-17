namespace Scrapper.Helpers;

using System.Text;
using System.Text.Encodings.Web;
using Enums;

public class UrlHelper
{
    public static readonly string BaseUrl = "https://jobs.dou.ua/";

    private static readonly IDictionary<PathEnum, string> Path = new Dictionary<PathEnum, string>()
    {
        { PathEnum.Vacancies, "vacancies" },
    };

    public static string BuildQuery(PathEnum path, IEnumerable<KeyValuePair<string, string>> queryParams)
    {
        if (!Path.TryGetValue(path, out string uri))
        {
            throw new ArgumentException("Invalid uri");
        }

        var sb = new StringBuilder();
        sb.Append(BaseUrl);
        sb.Append(uri);

        bool first = true;
            
        foreach (var parameter in queryParams)
        {
            if (string.IsNullOrEmpty(parameter.Value))
            {
                continue;
            }

            sb.Append(first ? '?': '&');
            sb.Append(UrlEncoder.Default.Encode(parameter.Key));
            sb.Append('=');
            sb.Append(UrlEncoder.Default.Encode(parameter.Value));
            first = false;
        }

        return sb.ToString();
    }
}