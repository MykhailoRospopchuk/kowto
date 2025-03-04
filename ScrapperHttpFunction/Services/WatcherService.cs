namespace ScrapperHttpFunction.Services;

using System.Collections.Concurrent;
using Helpers;
using Microsoft.Extensions.Logging;
using Models;
using Wrappers;

public class WatcherService
{
    private readonly ILogger<WatcherService> _logger;
    private List<ResourceConfig> resourceConfigs = [];

    public WatcherService(ILogger<WatcherService> logger)
    {
        _logger = logger;
    }

    public void AddConfig(ResourceConfig resourceConfig)
    {
        resourceConfigs.Add(resourceConfig);
    }
    
    public void AddConfig(List<ResourceConfig> resourceConfigs)
    {
        resourceConfigs.AddRange(resourceConfigs);
    }

    public async Task<List<JobListing>> ProcessResources()
    {
        try
        {
            ConcurrentStack<JobListing> jobs = new ConcurrentStack<JobListing>();
            List<Task> tasks = new List<Task>();

            foreach (var resource in resourceConfigs)
            {
                tasks.Add(Processing(resource, jobs));
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

    private async Task Processing(ResourceConfig resource, ConcurrentStack<JobListing> jobs)
    {
        try
        {
            using var client = ClientWrapper.GetInstance();
            var url = UrlHelper.BuildQuery(resource.Path, resource.Params);
            
            var response = await client.GetAsync<string>(url);
            
            if(!response.Success)
            {
                _logger.LogError(response.Exception, $"Failed to fetch data from the {resource.Path.ToString()} website");
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