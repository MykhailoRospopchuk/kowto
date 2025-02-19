using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ScrapperHttp.Function
{
    public class ScrapperHttpTrigger
    {
        private readonly ILogger<ScrapperHttpTrigger> _logger;

        public ScrapperHttpTrigger(ILogger<ScrapperHttpTrigger> logger)
        {
            _logger = logger;
        }

        [Function("ScrapperHttpTrigger")]
        public IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            return new OkObjectResult("Welcome to Azure Functions!");
        }
    }
}
