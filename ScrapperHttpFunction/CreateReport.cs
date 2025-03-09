namespace ScrapperHttpFunction;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Text;
using Azure;
using Azure.Storage.Blobs;
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
            
            var date = DateTime.UtcNow;

            // Load vacancy info from DB
            var existJobs = await _cosmoDbWrapper.GetRecords<JobInfo, JobInfoOutModel>(date.Day-1);
            var data = JsonConvert.SerializeObject(existJobs);
            var replaceText = $"data = {data};";
        
            // Load the report template
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = typeof(HtmlIndexAssembly).Assembly.GetManifestResourceNames().FirstOrDefault(x => x.Contains(HtmlTemplate));

            if (string.IsNullOrEmpty(resourceName))
            {
                _logger.LogError("The report template resource is not available.");
                return new BadRequestObjectResult("The report template resource is not available.");
            }

            var htmlTemplate = string.Empty;
            await using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream != null)
                {
                    using var reader = new StreamReader(stream);
                    htmlTemplate = await reader.ReadToEndAsync();
                }
            }

            // Populate the report template with the vacancy info
            var htmlBuilder = new StringBuilder(htmlTemplate);
            htmlBuilder.Replace(DataPlaceholder, replaceText);

            // Upload the report to the Azure Blob Storage
            var signature = Environment.GetEnvironmentVariable("AZURE_BLOB_CONTAINER_SIGNATURE") ??
                            throw new ArgumentNullException();
            var blobUri = Environment.GetEnvironmentVariable("AZURE_BLOB_CONTAINER_URI") ?? 
                          throw new ArgumentNullException();
            
            var sasCred = new AzureSasCredential(signature);
            var blobClient = new BlobClient(new Uri(blobUri + $"{date:yyyy-MM}/report.html"), sasCred);
            
            var byteArray = Encoding.UTF8.GetBytes(htmlBuilder.ToString());
            var ms = new MemoryStream(byteArray);
            
            await blobClient.UploadAsync(ms);
            
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