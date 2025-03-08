namespace ScrapperHttpFunction;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Text;
using Common.HtmlResources;
using CosmoDatabase.Entities;
using Wrappers;

public class CreateReport
{
    private readonly ILogger<CreateReport> _logger;
    private readonly CosmoDbWrapper _cosmoDbWrapper;

    public CreateReport(ILogger<CreateReport> logger, CosmoDbWrapper cosmoDbWrapper)
    {
        _logger = logger;
        _cosmoDbWrapper = cosmoDbWrapper;
    }

    [Function(nameof(CreateReport))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        
        var existJobs = await _cosmoDbWrapper.GetRecords<JobInfo, JobInfo>();
        
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = typeof(HtmlIndexAssembly).Assembly.GetManifestResourceNames().FirstOrDefault(x => x.Contains("ReportTemplate.html"));

        if (string.IsNullOrEmpty(resourceName))
        {
            return new BadRequestObjectResult("The report template resource is not available.");
        }

        using var stream = assembly.GetManifestResourceStream(resourceName);
        using var reader = new StreamReader(stream);

        var htmlBuilder = new StringBuilder(await reader.ReadToEndAsync());

        return new ContentResult()
        {
            Content = htmlBuilder.ToString(),
            ContentType = "text/html",
        };
    }
}