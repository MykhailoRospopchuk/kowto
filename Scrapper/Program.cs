namespace Scrapper;

using Enums;
using Helpers;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
        
        var client = new HttpClient();
        client.BaseAddress = new Uri(UrlHelper.BaseUrl);
        
        var queryParams = new Dictionary<string, string>
        {
            { "category", ".NET" },
            { "exp", "1-3" }
        };
            
        var uri = UrlHelper.BuildQuery(PathEnum.Vacancies, queryParams);
        
        var response = await client.GetAsync(uri);
        response.EnsureSuccessStatusCode();
        
        var html = await response.Content.ReadAsStringAsync();
        
        Console.ReadKey();
    }
}
