namespace ScrapperHttpFunction.Services;

using System.Collections.Concurrent;
using Helpers;
using Microsoft.Extensions.Logging;
using Models;
using Wrappers;

public class WatcherService
{
    private readonly ILogger<WatcherService> _logger;
    private readonly ClientWrapper _client;
    private List<ResourceConfig> _resourceConfigs = [];

    public WatcherService(ILogger<WatcherService> logger, ClientWrapper client)
    {
        _logger = logger;
        _client = client;

        _client.WithPipeline = true;
    }

    public void AddConfig(ResourceConfig resourceConfig)
    {
        _resourceConfigs.Add(resourceConfig);
    }

    public void AddConfig(List<ResourceConfig> resourceConfigs)
    {
        _resourceConfigs.AddRange([.. resourceConfigs.DistinctBy(x => x.Path)]);
    }

    public async Task<List<JobListing>> ProcessResources(CancellationToken cancellationToken)
    {
        try
        {
            ConcurrentStack<JobListing> jobs = new ConcurrentStack<JobListing>();
            List<Task> tasks = new List<Task>();

            foreach (var resource in _resourceConfigs)
            {
                tasks.Add(Processing(resource, jobs, cancellationToken));
            }

            await Task.WhenAll(tasks);

            return jobs.ToList();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to process resources");
            return new List<JobListing>();
        }
    }

    private async Task Processing(ResourceConfig resource, ConcurrentStack<JobListing> jobs, CancellationToken cancellationToken)
    {
        try
        {
            var url = UrlHelper.BuildQuery(resource.Path, resource.Params);

            var response = await _client.GetAsync<string>(url, cancellationToken);

            if(!response.Success)
            {
                _logger.LogError($"Failed to fetch data from the {resource.Path.ToString()} website");
            }
            else
            {
                var rawHtml = response.Value;
                var items = JobListingHelper.FetchJobListings(resource.Path, rawHtml);

                items.ForEach(jobs.Push);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Unexpected Error from the {resource.Path.ToString()} website");
        }
    }
}