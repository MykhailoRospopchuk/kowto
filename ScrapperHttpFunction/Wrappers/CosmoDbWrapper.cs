namespace ScrapperHttpFunction.Wrappers;

using System.Net;
using CosmoDatabase;
using CosmoDatabase.Entities;
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
    
    public async Task<List<T>> GetRecords<T>() where T : IKeyEntity
    {
        if (!_container.TryGetValue(typeof(T).AssemblyQualifiedName, out Container container))
        {
            _logger.LogError($"No container for type {typeof(T).AssemblyQualifiedName}");
            return new List<T>();
        }
        
        var query = "SELECT * FROM c";
        List<T> jobInfos = new List<T>();
        
        QueryDefinition queryDefinition = new QueryDefinition(query);
        using (FeedIterator<T> queryResultSetIterator = container.GetItemQueryIterator<T>(queryDefinition))
        {
            while (queryResultSetIterator.HasMoreResults)
            {
                FeedResponse<T> currentResultSet = await queryResultSetIterator.ReadNextAsync();

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
            _logger.LogError($"Received {e.StatusCode} ({e.Message}).");
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
        }

        return false;
    }
}