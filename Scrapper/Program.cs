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
        
        var rawHtml = await response.Content.ReadAsStringAsync();
        
        List<JobListing> jobs = JobListingHelper.FetchJobListings(rawHtml);

        // Display the extracted jobs
        foreach (var job in jobs)
        {
            Console.WriteLine($"Date: {job.Date}");
            Console.WriteLine($"Job Title: {job.Title}");
            Console.WriteLine($"Job URL: {job.Url}");
            Console.WriteLine($"Company Name: {job.CompanyName}");
            Console.WriteLine("------------------------------");
        }
        
        Console.ReadKey();
    }
}
