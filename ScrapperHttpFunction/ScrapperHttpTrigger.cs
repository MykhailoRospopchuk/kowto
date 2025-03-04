namespace ScrapperHttpFunction;

using CosmoDatabase.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Enums;
using Helpers;
using Models;
using Services;
using Wrappers;

public class VacancyScrapper
{
    private readonly ILogger<VacancyScrapper> _logger;
    private readonly CosmoDbWrapper _cosmoDbWrapper;
    private readonly LogicAppWrapper _logicAppWrapper;
    private readonly WatcherService _watcherService;

    public VacancyScrapper(
        ILogger<VacancyScrapper> logger,
        CosmoDbWrapper cosmoDbWrapper,
        LogicAppWrapper logicAppWrapper,
        WatcherService watcherService)
    {
        _logger = logger;
        _cosmoDbWrapper = cosmoDbWrapper;
        _logicAppWrapper = logicAppWrapper;
        _watcherService = watcherService;
    }

    // TODO: change to scheduled trigger
    [Function(nameof(VacancyScrapper))]
    public async Task<IActionResult> Run([TimerTrigger("0 0 6-22 * * *")] TimerInfo req)
    // public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");

        var queryParamsDou = new List<KeyValuePair<string, string>>
        {
            new ("category", ".NET"),
            new ("exp", "1-3")
        };

        var queryParamsDjinni = new List<KeyValuePair<string, string>>
        {
            new ("primary_keyword", ".NET"),
            new ("primary_keyword", "Dotnet Cloud"),
            new ("primary_keyword", "Dotnet Web"),
            new ("primary_keyword", "ASP.NET"),
            new ("exp_level", "1y"),
            new ("exp_level", "2y")
        };

        _watcherService.AddConfig(new ResourceConfig(PathEnum.DOU, queryParamsDou));
        _watcherService.AddConfig(new ResourceConfig(PathEnum.Djinni, queryParamsDjinni));

        List<JobListing> jobs = await _watcherService.ProcessResources();

        if (jobs.Count == 0)
        {
            return new OkObjectResult(new List<JobInfo>());
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