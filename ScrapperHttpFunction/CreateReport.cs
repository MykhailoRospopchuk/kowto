namespace ScrapperHttpFunction;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Text;
using Common.HtmlResources;
using CosmoDatabase.Entities;
using Models.DatabaseModels;
using Newtonsoft.Json;
using Wrappers;

public class CreateReport
{
    private readonly ILogger<CreateReport> _logger;
    private readonly CosmoDbWrapper _cosmoDbWrapper;

    private const string DataPlaceholder = "{{data = [];}}";
    private const string HtmlTemplate = "ReportTemplate.html";

    public CreateReport(ILogger<CreateReport> logger, CosmoDbWrapper cosmoDbWrapper)
    {
        _logger = logger;
        _cosmoDbWrapper = cosmoDbWrapper;
    }

    [Function(nameof(CreateReport))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
    {
        try
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
        
            var existJobs = await _cosmoDbWrapper.GetRecords<JobInfo, JobInfoOutModel>();
            var data = JsonConvert.SerializeObject(existJobs);
            var replaceText = $"data = {data};";
        
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = typeof(HtmlIndexAssembly).Assembly.GetManifestResourceNames().FirstOrDefault(x => x.Contains(HtmlTemplate));

            if (string.IsNullOrEmpty(resourceName))
            {
                _logger.LogError("The report template resource is not available.");
                return new BadRequestObjectResult("The report template resource is not available.");
            }

            string htmlTemplate;
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            using (var reader = new StreamReader(stream))
            {
                htmlTemplate = await reader.ReadToEndAsync();
            }

            var htmlBuilder = new StringBuilder(htmlTemplate);
            htmlBuilder.Replace(DataPlaceholder, replaceText);
            
            return new ContentResult()
            {
                Content = htmlBuilder.ToString(),
                ContentType = "text/html",
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An error occurred while generating the report.");
            return new BadRequestObjectResult("An error occurred while generating the report.");
        }
    }
}