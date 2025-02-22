using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ScrapperHttpFunction.CosmoDatabase;
using ScrapperHttpFunction.Wrappers;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

var connectionString = Environment.GetEnvironmentVariable("LogicAppWorkflowURL");
if (!string.IsNullOrEmpty(connectionString))
{
    throw new ArgumentException("Connection string is required.");
}
var container = new CosmoDbContainer(connectionString);
await container.Initialize();

builder.Services.AddSingleton(container);
builder.Services.AddTransient<CosmoDbWrapper>();

// Application Insights isn't enabled by default. See https://aka.ms/AAt8mw4.
// builder.Services
//     .AddApplicationInsightsTelemetryWorkerService()
//     .ConfigureFunctionsApplicationInsights();

builder.Build().Run();
