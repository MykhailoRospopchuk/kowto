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

        _client.WithPipeline = true;
    }

    public async Task CallLogicApp<T>(LogicAppRequest<T> request, CancellationToken cancellationToken)
    {
        var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

        var callLogicApp = await _client.PostAsync(_configuration.LogicAppUrl, content, cancellationToken);

        if(!callLogicApp.Success)
        {
            _logger.LogError("Logic App has not been triggered");
        }
    }
}