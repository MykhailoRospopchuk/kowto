namespace ScrapperHttpFunction.Wrappers;

using System.Net;
using CosmoDatabase;
using CosmoDatabase.Base;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

public class CosmoDbWrapper
{
    private IReadOnlyDictionary<string, Container> _container;
    private readonly ILogger<CosmoDbWrapper> _logger;

    public CosmoDbWrapper(CosmoDbContainer container, ILogger<CosmoDbWrapper> logger)
    {
        _container = container.GetContainer;
        _logger = logger;
    }
    
    public async Task AddRecords<T>(List<T> jobs) where T : IKeyEntity
    {
        List<Task> tasks = new List<Task>(jobs.Count);

        if (!_container.TryGetValue(typeof(T).AssemblyQualifiedName, out Container container))
        {
            _logger.LogError($"No container for type {typeof(T).AssemblyQualifiedName}");
            return;
        }

        foreach (T item in jobs)
        {
            tasks.Add(container.CreateItemAsync<T>(item, new PartitionKey(item.Id))
                .ContinueWith(itemResponse =>
                {
                    if (!itemResponse.IsCompletedSuccessfully)
                    {
                        AggregateException innerExceptions = itemResponse.Exception.Flatten();
                        if (innerExceptions.InnerExceptions.FirstOrDefault(innerEx => innerEx is CosmosException) is CosmosException cosmosException)
                        {
                            _logger.LogError($"Received {cosmosException.StatusCode} ({cosmosException.Message}).");
                        }
                        else
                        {
                            _logger.LogError($"Exception {innerExceptions.InnerExceptions.FirstOrDefault()}.");
                        }
                    }
                }));
        }

        await Task.WhenAll(tasks);
    }

    public async Task<List<TOut>> GetRecords<T, TOut>(int range = 0) where T : IKeyEntity where TOut : IOutType
    {
        if (!_container.TryGetValue(typeof(T).AssemblyQualifiedName, out Container container))
        {
            _logger.LogError($"No container for type {typeof(T).AssemblyQualifiedName}");
            return new List<TOut>();
        }

        var query = "SELECT * FROM c";
        QueryDefinition queryDefinition = new QueryDefinition(query);
        if (range > 0)
        {
            var beginRange = DateTimeOffset.UtcNow.AddDays(-range).ToUnixTimeSeconds();
            query = "SELECT * FROM c WHERE c._ts > @timestamp";
            queryDefinition = new QueryDefinition(query).WithParameter("@timestamp", beginRange);
        }

        List<TOut> jobInfos = new List<TOut>();
        using (FeedIterator<TOut> queryResultSetIterator = container.GetItemQueryIterator<TOut>(queryDefinition))
        {
            while (queryResultSetIterator.HasMoreResults)
            {
                FeedResponse<TOut> currentResultSet = await queryResultSetIterator.ReadNextAsync();

                jobInfos.AddRange(currentResultSet);
            }
        }

        return jobInfos;
    }
    
    public async Task<bool> DeleteRecord<T>(string id) where T : IKeyEntity
    {
        try
        {
            if (!_container.TryGetValue(typeof(T).AssemblyQualifiedName, out Container container))
            {
                _logger.LogError($"No container for type {typeof(T).AssemblyQualifiedName}");
                return false;
            }
            
            var result = await container.DeleteItemAsync<T>(id, new PartitionKey(id));
            return result.StatusCode == HttpStatusCode.OK;
        }
        catch (CosmosException e)
        {
            _logger.LogError("Could not delete job listing with id {id}", id);
            _logger.LogError($"Received Code:{e.StatusCode} Message: ({e.Message}).");
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
        }

        return false;
    }
}