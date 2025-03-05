namespace ScrapperHttpFunction;

using Constant;
using CosmoDatabase.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Enums;
using Helpers;
using Microsoft.AspNetCore.Http;
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
        _logger.LogInformation(Environment.GetEnvironmentVariable(FunctionEnviroment.AZURE_FUNCTIONS_ENVIRONMENT));

        var configs = await _cosmoDbWrapper.GetRecords<Resource>();

        if (!configs.Any())
        {
            return new OkObjectResult(new List<JobInfo>());
        }
        
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
            new ("primary_keyword", "Blazor"),
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

        var existJobs = await _cosmoDbWrapper.GetRecords<JobInfo>();

        var processingResult = JobProcessingHelper.ProcessJobListings(jobs, existJobs);

        foreach (var item in processingResult.toRemove)
        {
            await _cosmoDbWrapper.DeleteRecord<JobInfo>(item.Id);
        }

        if (processingResult.toAdd.Any())
        {
            await _cosmoDbWrapper.AddRecords(processingResult.toAdd);

            if (Environment.GetEnvironmentVariable(FunctionEnviroment.AZURE_FUNCTIONS_ENVIRONMENT) == FunctionEnviroment.Development)
            {
                // Display the extracted jobs
                foreach (var job in processingResult.toAdd)
                {
                    _logger.LogInformation($"Date: {job.Date}\nJob Title: {job.Title}\nJob URL: {job.Url}\nCompany Name: {job.CompanyName}\n------------------------------");
                }
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