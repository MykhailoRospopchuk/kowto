namespace ScrapperHttpFunction.Wrappers;

using System.Text;
using Common.Configurations;
using CosmoDatabase.Entities;
using FunctionRequestDTO;
using Helpers;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

public class LogicAppWrapper
{
    private readonly ILogger<LogicAppWrapper> _logger;
    private readonly ClientWrapper _client;
    private readonly CommunicationLogicAppConfiguration _configuration;
    private readonly CosmoDbWrapper _cosmoDb;

    public LogicAppWrapper(
        ILogger<LogicAppWrapper> logger,
        ClientWrapper client,
        CommunicationLogicAppConfiguration configuration,
        CosmoDbWrapper cosmoDb)
    {
        _logger = logger;
        _client = client;
        _configuration = configuration;
        _cosmoDb = cosmoDb;

        _client.WithPipeline = true;
    }

    public async Task CallLogicApp<T>(LogicAppRequest<T> request, CancellationToken cancellationToken)
    {
        var serializedRequest = JsonConvert.SerializeObject(request);
        var hash = HashHelper.GetHashMd5(new[] { serializedRequest });

        // Check do already exist RequestId in Cosmos DB. If do not exist continue
        var requestIdExist = await _cosmoDb.RecordExists<RequestsId>(hash, cancellationToken);

        if (requestIdExist.Value)
        {
            _logger.LogWarning("Record with id {RecordId} already exists, and the request has most likely already been executed", hash);
            return;
        }

        var content = new StringContent(serializedRequest, Encoding.UTF8, "application/json");
        content.Headers.Add("requestId", hash);

        var callLogicApp = await _client.PostAsync<string>(_configuration.LogicAppUrl, content, cancellationToken);

        if(!callLogicApp.Success)
        {
            _logger.LogError("Logic App has not been triggered");
        }

        if (string.IsNullOrEmpty(callLogicApp.Value))
        {
            _logger.LogError("Unexpected response from logic app missing RequestId");
        }

        if (callLogicApp.Value == hash)
        {
            // Logic app return valid RequestId - email successfully send. Add RequestId to Database
            await _cosmoDb.AddRecord<RequestsId>(new RequestsId
            {
                Id = hash
            }, cancellationToken);
        }
    }
}