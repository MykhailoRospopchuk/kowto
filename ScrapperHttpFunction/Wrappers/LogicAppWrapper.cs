namespace ScrapperHttpFunction.Wrappers;

using System.Text;
using CosmoDatabase.Entities;
using Helpers;
using Microsoft.Extensions.Logging;

public class LogicAppWrapper
{
    private readonly ILogger<LogicAppWrapper> _logger;

    public LogicAppWrapper(ILogger<LogicAppWrapper> logger)
    {
        _logger = logger;
    }

    public async Task<bool> CallLogicApp(List<JobInfo> jobList)
    {
        using var client = ClientWrapper.GetInstance();
        var logicAppUrl = Environment.GetEnvironmentVariable("LogicAppWorkflowURL");

        var content = new StringContent(HtmlMessageHelper.BuildHtml(jobList), Encoding.UTF8, "text/html");

        var callLogicApp = await client.PostAsync(logicAppUrl, content);

        if(!callLogicApp.Success)
        {
            _logger.LogError("Logic App has not been triggered");
        }

        return callLogicApp.Success;
    }
}