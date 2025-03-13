using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using ScrapperHttpFunction.Common.Configurations;
using ScrapperHttpFunction.CosmoDatabase;
using ScrapperHttpFunction.Services;
using ScrapperHttpFunction.Wrappers;

var builder = FunctionsApplication.CreateBuilder(args);

var loggerBuilder = builder.Logging;
var logger = loggerBuilder.Services.BuildServiceProvider().GetService<ILogger<Program>>();

builder.ConfigureFunctionsWebApplication();

// Known issues. Compatibility with .NET Application Insights 
// https://github.com/dotnet/extensions/blob/main/src/Libraries/Microsoft.Extensions.Http.Resilience/README.md#compatibility-with-net-application-insights
builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

try
{
    var logicAppUrl = 
        Environment.GetEnvironmentVariable("CommunicationLogicApp") ??
        throw new ArgumentException("LogicAppWorkflowURL environment variable is not set");
    var connectionString = 
        Environment.GetEnvironmentVariable("CosmoConnectionString") ?? 
        throw new ArgumentException("CosmoConnectionString environment variable is not set");
    var signature = 
        Environment.GetEnvironmentVariable("AZURE_BLOB_CONTAINER_SIGNATURE") ??
        throw new ArgumentNullException();
    var containerUri = 
        Environment.GetEnvironmentVariable("AZURE_BLOB_CONTAINER_URI") ?? 
        throw new ArgumentNullException();
    
    var container = new CosmoDbContainer(connectionString);
    await container.Initialize();

    builder.Services.AddSingleton(new AzureBlobContainerConfiguration
    {
        Signature = signature,
        ContainerUri = containerUri
    });
    
    builder.Services.AddSingleton(new CommunicationLogicAppConfiguration
    {
        LogicAppUrl = logicAppUrl
    });

    builder.Services.AddSingleton(container);
    builder.Services.AddTransient<CosmoDbWrapper>();
    builder.Services.AddTransient<LogicAppWrapper>();
    builder.Services.AddTransient<WatcherService>();
    builder.Services.AddScoped<ClientWrapper>();

    builder.Services.AddHttpClient<ClientWrapper>().AddStandardResilienceHandler(options => {
        // Customize retry strategy
        options.Retry.MaxRetryAttempts = 3;
        options.Retry.BackoffType = DelayBackoffType.Exponential;
        options.Retry.Delay = TimeSpan.FromSeconds(1);

        // Customize circuit breaker strategy
        options.CircuitBreaker.FailureRatio = 0.1; // 10% failure rate
        options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
        options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);

        // Customize total request timeout
        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(60);
    });
}
catch (Exception ex)
{
    logger.LogError(ex.Message);
    logger.LogInformation("App shut down");
    return;
}

var host = builder.Build();
host.Run();
