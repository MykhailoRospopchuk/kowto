using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using ScrapperHttpFunction.Enums;
using ScrapperHttpFunction.Helpers;
using ScrapperHttpFunction.Models;
using ScrapperHttpFunction.Wrappers;

namespace ScrapperHttpFunction
{
    public class ScrapperHttpTrigger
    {
        private readonly ILogger<ScrapperHttpTrigger> _logger;
        private readonly CosmoDbWrapper _cosmoDbWrapper;

        public ScrapperHttpTrigger(ILogger<ScrapperHttpTrigger> logger, CosmoDbWrapper cosmoDbWrapper)
        {
            _logger = logger;
            _cosmoDbWrapper = cosmoDbWrapper;
        }

        [Function("ScrapperHttpTrigger")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            using var client = ClientWrapper.GetInstance();

            var queryParams = new Dictionary<string, string>
            {
                { "category", ".NET" },
                { "exp", "1-3" }
            };

            var uri = UrlHelper.BuildQuery(PathEnum.Vacancies, queryParams);

            var response = await client.GetAsync<string>(uri);

            if(!response.Success)
            {
                _logger.LogError(response.Exception, "Failed to fetch data from the website");
                return new BadRequestObjectResult("Failed to fetch data from the website");
            }

            var rawHtml = response.Value;

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

                if(!callLogicApp.Success)
                {
                    _logger.LogError("Logic App has not been triggered");
                    return new BadRequestObjectResult("Logic App has not been triggered");
                }
            }

            return new OkObjectResult(jobs);
        }
    }
}
