namespace ScrapperHttpFunction.Wrappers;

using System.Net;
using CosmoDatabase;
using CosmoDatabase.Entities;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

public class CosmoDbWrapper
{
    private Container _container;
    private readonly ILogger<CosmoDbWrapper> _logger;

    public CosmoDbWrapper(CosmoDbContainer container, ILogger<CosmoDbWrapper> logger)
    {
        _container = container.GetContainer;
        _logger = logger;
    }
    
    public async Task AddJobListing(List<JobInfo> jobs)
    {
        List<Task> tasks = new List<Task>(jobs.Count);

        foreach (JobInfo item in jobs)
        {
            tasks.Add(_container.CreateItemAsync<JobInfo>(item, new PartitionKey(item.Id))
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
    
    public async Task<List<JobInfo>> GetJobListings()
    {
        var query = "SELECT * FROM c";
        
        QueryDefinition queryDefinition = new QueryDefinition(query);
        FeedIterator<JobInfo> queryResultSetIterator = _container.GetItemQueryIterator<JobInfo>(queryDefinition);

        List<JobInfo> jobInfos = new List<JobInfo>();

        while (queryResultSetIterator.HasMoreResults)
        {
            FeedResponse<JobInfo> currentResultSet = await queryResultSetIterator.ReadNextAsync();

            jobInfos.AddRange(currentResultSet);
        }

        return jobInfos;
    }
    
    public async Task<bool> DeleteJobListing(string id)
    {
        try
        {
            var result = await _container.DeleteItemAsync<JobInfo>(id, new PartitionKey(id));
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