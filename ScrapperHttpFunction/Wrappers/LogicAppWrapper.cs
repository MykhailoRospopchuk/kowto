namespace ScrapperHttpFunction.Wrappers;

using System.Text;
using Common.Configurations;
using FunctionRequestDTO;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

public class LogicAppWrapper
{
    private readonly ILogger<LogicAppWrapper> _logger;
    private readonly ClientWrapper _client;
    private readonly CommunicationLogicAppConfiguration _configuration;

    public LogicAppWrapper(
        ILogger<LogicAppWrapper> logger,
        ClientWrapper client,
        CommunicationLogicAppConfiguration configuration)
    {
        _logger = logger;
        _client = client;
        _configuration = configuration;
    }

    public async Task<bool> CallLogicApp<T>(LogicAppRequest<T> request)
    {
        var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

        var callLogicApp = await _client.PostAsync(_configuration.LogicAppUrl, content);

        if(!callLogicApp.Success)
        {
            _logger.LogError("Logic App has not been triggered");
            _logger.LogError(callLogicApp.Exception.Message);
        }

        return callLogicApp.Success;
    }
}