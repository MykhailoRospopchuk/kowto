namespace ScrapperHttpFunction
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.Functions.Worker;
    using Microsoft.Extensions.Logging;
    using Enums;
    using Helpers;
    using Models;
    using Wrappers;

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
        // public async Task<IActionResult> Run([TimerTrigger("0 0 6-22 * * *")] TimerInfo req)
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            List<JobListing> jobs = new List<JobListing>();

            using var client = ClientWrapper.GetInstance();

            var queryParamsDou = new Dictionary<string, string>
            {
                { "category", ".NET" },
                { "exp", "1-3" }
            };

            var uriDou = UrlHelper.BuildQuery(PathEnum.DOU, queryParamsDou);

            var responseDou = await client.GetAsync<string>(uriDou);
            
            if(!responseDou.Success)
            {
                _logger.LogError(responseDou.Exception, "Failed to fetch data from the DOU website");
            }
            else
            {
                var rawHtml = responseDou.Value;
                jobs.AddRange(JobListingHelper.FetchJobListings(PathEnum.DOU, rawHtml));
            }
            
            var queryParamsDjinni = new List<KeyValuePair<string, string>>
            {
                new ("primary_keyword", ".NET"),
                new ("primary_keyword", "Dotnet Cloud"),
                new ("primary_keyword", "Dotnet Web"),
                new ("primary_keyword", "ASP.NET"),
                new ("exp_level", "1y"),
                new ("exp_level", "2y")
            };

            var uriDjinni = UrlHelper.BuildQuery(PathEnum.Djinni, queryParamsDjinni);

            var responseDjinni = await client.GetAsync<string>(uriDjinni);
            
            if(!responseDjinni.Success)
            {
                _logger.LogError(responseDou.Exception, "Failed to fetch data from the Djinni website");
            }
            else
            {
                var rawHtml = responseDjinni.Value;
                jobs.AddRange(JobListingHelper.FetchJobListings(PathEnum.Djinni, rawHtml));
            }

            if (!responseDou.Success && !responseDjinni.Success)
            {
                return new BadRequestObjectResult("Failed to fetch data from the website");
            }

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
