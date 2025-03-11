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
using FunctionRequestDTO;
using Models.DatabaseModels;
using Newtonsoft.Json;
using Wrappers;

public class CreateReport
{
    private readonly ILogger<CreateReport> _logger;
    private readonly CosmoDbWrapper _cosmoDbWrapper;
    private readonly LogicAppWrapper _logicAppWrapper;

    private const string DataPlaceholder = "{{data = [];}}";
    private const string TargetMonth = "{{target_month}}";
    private const string HtmlTemplate = "ReportTemplate.html";

    public CreateReport(ILogger<CreateReport> logger, CosmoDbWrapper cosmoDbWrapper, LogicAppWrapper logicAppWrapper)
    {
        _logger = logger;
        _cosmoDbWrapper = cosmoDbWrapper;
        _logicAppWrapper = logicAppWrapper;
    }

    [Function(nameof(CreateReport))]
    // public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
    public async Task<IActionResult> Run([TimerTrigger("0 0 5 * * *")] TimerInfo req)
    {
        try
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var date = DateTime.UtcNow.AddDays(-1);

            // Load vacancy info from DB
            var existJobs = await _cosmoDbWrapper.GetRecords<JobInfo, JobInfoOutModel>(date.Day);
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
            htmlBuilder.Replace(TargetMonth, $"{date:yyyy-MM}");

            // Upload the report to the Azure Blob Storage
            var signature = Environment.GetEnvironmentVariable("AZURE_BLOB_CONTAINER_SIGNATURE") ??
                            throw new ArgumentNullException();
            var containerUri = Environment.GetEnvironmentVariable("AZURE_BLOB_CONTAINER_URI") ?? 
                          throw new ArgumentNullException();

            var sasCred = new AzureSasCredential(signature);
            var blobUri = new Uri(containerUri + $"{date:yyyy-MM}/{date:yyyy-MM}-report.html");
            var blobClient = new BlobClient(blobUri, sasCred);

            var byteArray = Encoding.UTF8.GetBytes(htmlBuilder.ToString());
            var ms = new MemoryStream(byteArray);

            await blobClient.UploadAsync(ms, overwrite: true);

            // Send email with url to report
            var callLogicApp = await _logicAppWrapper.CallLogicApp(new LogicAppRequest<string>
            {
                Title = "Vacancy report for the current month",
                Content = blobUri.ToString()
            });

            if(!callLogicApp)
            {
                _logger.LogError("Logic App has not been triggered");
            }

            return new OkObjectResult(blobUri.ToString());
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
            return new BadRequestObjectResult("An error occurred while generating the report.");
        }
    }
}