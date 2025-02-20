using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using ScrapperHttpFunction.Enums;
using ScrapperHttpFunction.Helpers;
using ScrapperHttpFunction.Models;

namespace ScrapperHttp.Function
{
    public class ScrapperHttpTrigger
    {
        private readonly ILogger<ScrapperHttpTrigger> _logger;

        public ScrapperHttpTrigger(ILogger<ScrapperHttpTrigger> logger)
        {
            _logger = logger;
        }

        [Function("ScrapperHttpTrigger")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            using var client = new HttpClient();
            
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
                _logger.LogInformation($"Date: {job.Date}\nJob Title: {job.Title}\nJob URL: {job.Url}\nCompany Name: {job.CompanyName}\n------------------------------");
            }

            var logicAppUrl = Environment.GetEnvironmentVariable("LogicAppWorkflowURL");

            if(!string.IsNullOrEmpty(logicAppUrl))
            {
                var content = new StringContent(JsonSerializer.Serialize(jobs.FirstOrDefault()), Encoding.UTF8, "application/json");

                var callLogicApp = await client.PostAsync(logicAppUrl, content);
                callLogicApp.EnsureSuccessStatusCode();

                if(!callLogicApp.IsSuccessStatusCode)
                {
                    _logger.LogError("Logic App has not been triggered");
                }
            }
            
            return new OkObjectResult(jobs);
            // return new OkObjectResult("Welcome to Azure Functions!");
        }
    }
}
