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
    public class VacancyScrapper
    {
        private readonly ILogger<VacancyScrapper> _logger;
        private readonly CosmoDbWrapper _cosmoDbWrapper;
        private readonly LogicAppWrapper _logicAppWrapper;

        public VacancyScrapper(
            ILogger<VacancyScrapper> logger,
            CosmoDbWrapper cosmoDbWrapper,
            LogicAppWrapper logicAppWrapper)
        {
            _logger = logger;
            _cosmoDbWrapper = cosmoDbWrapper;
            _logicAppWrapper = logicAppWrapper;
        }

        // TODO: change to scheduled trigger
        [Function(nameof(VacancyScrapper))]
        public async Task<IActionResult> Run([TimerTrigger("0 0 6-22 * * *")] TimerInfo req)
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

            var existJobs = await _cosmoDbWrapper.GetJobListings();

            var processingResult = JobProcessingHelper.ProcessJobListings(jobs, existJobs);

            foreach (var item in processingResult.toRemove)
            {
                await _cosmoDbWrapper.DeleteJobListing(item.Id);
            }

            if (processingResult.toAdd.Any())
            {
                await _cosmoDbWrapper.AddJobListing(processingResult.toAdd);

                // Display the extracted jobs
                foreach (var job in processingResult.toAdd)
                {
                    _logger.LogInformation($"Date: {job.Date}\nJob Title: {job.Title}\nJob URL: {job.Url}\nCompany Name: {job.CompanyName}\n------------------------------");
                }

                var callLogicApp = await _logicAppWrapper.CallLogicApp(processingResult.toAdd);

                if(!callLogicApp)
                {
                    return new BadRequestObjectResult("Logic App has not been triggered");
                }
            }

            return new OkObjectResult(processingResult.toAdd);
        }
    }
}
