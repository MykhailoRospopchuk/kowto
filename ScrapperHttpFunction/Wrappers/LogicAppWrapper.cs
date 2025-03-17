namespace ScrapperHttpFunction.Wrappers;

using System.Text;
using Common.Configurations;
using FunctionRequestDTO;
using Helpers;
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
        var serializedRequest = JsonConvert.SerializeObject(request);
        var hash = HashHelper.GetHashMd5(new[] { serializedRequest });
        // TODO: Check do already exist RequestId in Cosmos DB. If do not exist continue

        var content = new StringContent(serializedRequest, Encoding.UTF8, "application/json");
        content.Headers.Add("requestId", hash);

        var callLogicApp = await _client.PostAsync<string>(_configuration.LogicAppUrl, content, cancellationToken);

        if(!callLogicApp.Success)
        {
            _logger.LogError("Logic App has not been triggered");
        }

        if (callLogicApp.Value == hash)
        {
            // TODO: Add RequestId to Cosmos DB
        }
    }
}