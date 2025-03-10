namespace ScrapperHttpFunction;

using Constant;
using CosmoDatabase.Entities;
using FunctionRequestDTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
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

        var configs = await _cosmoDbWrapper.GetRecords<Resource, Resource>();

        if (!configs.Any())
        {
            return new OkObjectResult(new List<JobInfo>());
        }

        var resourceConfigs = configs
            .Select(x => new ResourceConfig(x.Path, x.Params))
            .ToList();

        _watcherService.AddConfig(resourceConfigs);

        List<JobListing> jobs = await _watcherService.ProcessResources();

        if (jobs.Count == 0)
        {
            return new OkObjectResult(new List<JobInfo>());
        }

        var existJobs = await _cosmoDbWrapper.GetRecords<JobInfo, JobInfo>();

        var processingResult = JobProcessingHelper.ToAdd(jobs, existJobs);

        if (processingResult.Any())
        {
            await _cosmoDbWrapper.AddRecords(processingResult);

            if (Environment.GetEnvironmentVariable(FunctionEnviroment.AZURE_FUNCTIONS_ENVIRONMENT) == FunctionEnviroment.Development)
            {
                // Display the extracted jobs
                foreach (var job in processingResult)
                {
                    _logger.LogInformation($"Date: {job.Date}\nJob Title: {job.Title}\nJob URL: {job.Url}\nCompany Name: {job.CompanyName}\n------------------------------");
                }
            }

            var callLogicApp = await _logicAppWrapper.CallLogicApp(new LogicAppRequest<string>
            {
                Title = "Attention! New vacancy has been discovered",
                Content = HtmlMessageHelper.BuildHtml(processingResult)
            });

            if(!callLogicApp)
            {
                _logger.LogError("Logic App has not been triggered");
            }
        }

        return new OkObjectResult(processingResult);
    }
}