using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ScrapperHttpFunction.CosmoDatabase;
using ScrapperHttpFunction.Services;
using ScrapperHttpFunction.Wrappers;

var builder = FunctionsApplication.CreateBuilder(args);

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

builder.ConfigureFunctionsWebApplication();

// Application Insights isn't enabled by default. See https://aka.ms/AAt8mw4.
builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Build().Run();
