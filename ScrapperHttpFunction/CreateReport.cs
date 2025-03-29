namespace ScrapperHttpFunction;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Text;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Common.Configurations;
using Common.HtmlResources;
using CosmoDatabase.Entities;
using FunctionRequestDTO;
using Helpers;
using Models.DatabaseModels;
using Newtonsoft.Json;
using Wrappers;

public class CreateReport
{
    private readonly ILogger<CreateReport> _logger;
    private readonly CosmoDbWrapper _cosmoDbWrapper;
    private readonly LogicAppWrapper _logicAppWrapper;
    private readonly AzureBlobContainerConfiguration _blobConfiguration;

    private readonly DateTime _date;
    private string _dataContent;
    private string _report;
    private string _reportUrl;
    private int _count;

    private const string DataPlaceholder = "{{data = [];}}";
    private const string TargetMonth = "{{target_month}}";
    private const string HtmlTemplate = "ReportTemplate.html";

    private string GetPopulatedData(string data) => $"data = {data};";

    public CreateReport(
        ILogger<CreateReport> logger,
        CosmoDbWrapper cosmoDbWrapper,
        LogicAppWrapper logicAppWrapper,
        AzureBlobContainerConfiguration blobConfiguration)
    {
        _logger = logger;
        _cosmoDbWrapper = cosmoDbWrapper;
        _logicAppWrapper = logicAppWrapper;
        _blobConfiguration = blobConfiguration;

        _date = DateTime.UtcNow.AddDays(-1);
    }

    [Function(nameof(CreateReport))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req, CancellationToken cancellationToken)
    // public async Task<IActionResult> Run([TimerTrigger("0 0 5 * * *")] TimerInfo req, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            await LoadData();
            await LoadTemplate();
            await UploadReport();
            await SendEmail(cancellationToken);

            return new OkObjectResult(_reportUrl);
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
            return new BadRequestObjectResult("An error occurred while generating the report.");
        }
    }

    private async Task LoadData()
    {
        // Load vacancy info from DB
        var data = await _cosmoDbWrapper.GetRecords<JobInfo, JobInfoOutModel>(_date.Day);

        _count = data.Count(j => j.TimestampUnix > DateTimeOffset.UtcNow.AddDays(-1).ToUnixTimeSeconds());

        _dataContent = JsonConvert.SerializeObject(data);
    }

    private async Task LoadTemplate()
    {
        var htmlTemplate = await GetHtmlTemplate();
        PopulateTemplate(htmlTemplate);
    }

    private async Task<string> GetHtmlTemplate()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = typeof(HtmlIndexAssembly).Assembly.GetManifestResourceNames().FirstOrDefault(x => x.Contains(HtmlTemplate));

        if (string.IsNullOrEmpty(resourceName))
        {
            throw new Exception($"The report template resource is not available: {HtmlTemplate}");
        }

        await using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new Exception("Failed to load the report template stream.");
        }

        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    private void PopulateTemplate(string htmlTemplate)
    {
        var htmlBuilder = new StringBuilder(htmlTemplate);
        htmlBuilder.Replace(DataPlaceholder, GetPopulatedData(_dataContent));
        htmlBuilder.Replace(TargetMonth, $"{_date:yyyy-MM}");
        _report = htmlBuilder.ToString();
    }

    private async Task UploadReport()
    {
        var sskCredentials = new StorageSharedKeyCredential(_blobConfiguration.StorageAccountName, _blobConfiguration.StorageAccountKey);

        var blobName = $"{_date:yyyy-MM}/{_date:yyyy-MM}-report.html";
        var blobUri = new Uri(_blobConfiguration.StorageContainerUrl + blobName);
        var blobClient = new BlobClient(blobUri, sskCredentials);

        var byteArray = Encoding.UTF8.GetBytes(_report);
        var ms = new MemoryStream(byteArray);

        await blobClient.UploadAsync(ms, overwrite: true);

        if (blobClient.CanGenerateSasUri)
        {
            var sasBuilder = new BlobSasBuilder
            {
                Protocol = SasProtocol.Https,
                StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
                ExpiresOn = DateTimeOffset.UtcNow.AddHours(20),
                IPRange = new SasIPRange(System.Net.IPAddress.None, System.Net.IPAddress.None),
                BlobContainerName = blobClient.BlobContainerName,
                BlobName = blobName,
                Resource = "b",
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            var signedUrl = blobClient.GenerateSasUri(sasBuilder);
            _reportUrl = signedUrl.ToString();
        }
    }

    private async Task SendEmail(CancellationToken cancellationToken)
    {
        // Send email with url to report
        await _logicAppWrapper.CallLogicApp(new LogicAppRequest<string>
        {
            Title = "Vacancy report for the current month",
            Content = HtmlMessageHelper.BuildHtml(_count, _reportUrl)
        }, cancellationToken);
    }
}