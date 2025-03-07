using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using ScrapperHttpFunction.CosmoDatabase;
using ScrapperHttpFunction.Services;
using ScrapperHttpFunction.Wrappers;

var builder = FunctionsApplication.CreateBuilder(args);

var loggerBuilder = builder.Logging;
var logger = loggerBuilder.Services.BuildServiceProvider().GetService<ILogger<Program>>();

try
{
    var logicAppUrl = Environment.GetEnvironmentVariable("LogicAppWorkflowURL");
    if (string.IsNullOrEmpty(logicAppUrl))
    {
        throw new ArgumentException("LogicAppWorkflowURL environment variable is not set");
    }
    var connectionString = Environment.GetEnvironmentVariable("CosmoConnectionString");
    if (string.IsNullOrEmpty(connectionString))
    {
        throw new ArgumentException("Connection string is required.");
    }
    
    var container = new CosmoDbContainer(connectionString);
    await container.Initialize();
    
    builder.Services.AddSingleton(container);
    builder.Services.AddTransient<CosmoDbWrapper>();
    builder.Services.AddTransient<LogicAppWrapper>();
    builder.Services.AddTransient<WatcherService>();
    builder.Services.AddScoped<ClientWrapper>();

    builder.Services.AddHttpClient<ClientWrapper>().AddStandardResilienceHandler(options => {
        // Customize retry strategy
        options.Retry.MaxRetryAttempts = 3;
        options.Retry.BackoffType = DelayBackoffType.Exponential;
        options.Retry.Delay = TimeSpan.FromSeconds(2);

        // Customize circuit breaker strategy
        options.CircuitBreaker.FailureRatio = 0.1; // 10% failure rate
        options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
        options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);

        // Customize total request timeout
        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(40);
    });
}
catch (Exception ex)
{
    logger.LogError(ex.Message);
    logger.LogInformation("App shut down");
    return;
}

builder.ConfigureFunctionsWebApplication();

// Application Insights isn't enabled by default. See https://aka.ms/AAt8mw4.
builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

var host = builder.Build();
host.Run();
