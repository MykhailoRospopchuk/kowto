namespace ScrapperHttpFunction.Wrappers;

using System.Text;
using FunctionRequestDTO;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

public class LogicAppWrapper
{
    private readonly ILogger<LogicAppWrapper> _logger;
    private readonly ClientWrapper _client;

    public LogicAppWrapper(ILogger<LogicAppWrapper> logger, ClientWrapper client)
    {
        _logger = logger;
        _client = client;
    }

    public async Task<bool> CallLogicApp<T>(LogicAppRequest<T> request)
    {
        // temporary outlook account was suspended so we can try another feature using azure communication service
        // var logicAppUrl = Environment.GetEnvironmentVariable("LogicAppWorkflowURL"); 
        var logicAppUrl = Environment.GetEnvironmentVariable("CommunicationLogicApp");

        var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

        var callLogicApp = await _client.PostAsync(logicAppUrl, content);

        if(!callLogicApp.Success)
        {
            _logger.LogError("Logic App has not been triggered");
        }

        return callLogicApp.Success;
    }
}