namespace ScrapperHttpFunction.Wrappers;

using System.Text;
using CosmoDatabase.Entities;
using Helpers;
using Microsoft.Extensions.Logging;

public class LogicAppWrapper
{
    private readonly ILogger<LogicAppWrapper> _logger;
    private readonly ClientWrapper _client;

    public LogicAppWrapper(ILogger<LogicAppWrapper> logger, ClientWrapper client)
    {
        _logger = logger;
        _client = client;
    }

    public async Task<bool> CallLogicApp(List<JobInfo> jobList)
    {
        // temporary outlook account was suspended so we can try another feature using azure communication service
        // var logicAppUrl = Environment.GetEnvironmentVariable("LogicAppWorkflowURL"); 
        var logicAppUrl = Environment.GetEnvironmentVariable("CommunicationLogicApp");

        var content = new StringContent(HtmlMessageHelper.BuildHtml(jobList), Encoding.UTF8, "text/html");

        var callLogicApp = await _client.PostAsync(logicAppUrl, content);

        if(!callLogicApp.Success)
        {
            _logger.LogError("Logic App has not been triggered");
        }

        return callLogicApp.Success;
    }
}