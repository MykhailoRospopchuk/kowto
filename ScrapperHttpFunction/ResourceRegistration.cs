namespace ScrapperHttpFunction;

using CosmoDatabase.Entities;
using FunctionRequestDTO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Wrappers;

public class ResourceRegistration
{
    private readonly ILogger<ResourceRegistration> _logger;
    private readonly CosmoDbWrapper _cosmoDbWrapper;

    public ResourceRegistration(ILogger<ResourceRegistration> logger, CosmoDbWrapper cosmoDbWrapper)
    {
        _logger = logger;
        _cosmoDbWrapper = cosmoDbWrapper;
    }

    [Function(nameof(ResourceRegistration))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request for Resource Registration.");
        try
        {
            if (req.Method == HttpMethods.Get)
            {
                var resources = await _cosmoDbWrapper.GetRecords<Resource>();
                return new OkObjectResult(resources);
            }

            var request = await req.ReadFromJsonAsync<ResourceRegistrationRequestDTO>();

            foreach (var id in request.DeleteRecordIds)
            {
                await _cosmoDbWrapper.DeleteRecord<Resource>(id);
            }

            if (!request.Resources.Any())
            {
                return new OkObjectResult("Success!");
            }

            var resourcesToAdd = request.Resources.Select(r => 
                    new Resource
                    {
                        Id = Ulid.NewUlid().ToString(),
                        Path = r.Path,
                        Params = r.Params
                    })
                .ToList();

            await _cosmoDbWrapper.AddRecords<Resource>(resourcesToAdd);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error occured while processing a request.");
            return new BadRequestObjectResult(e);
        }

        return new OkObjectResult("Success!");
    }
}

// {
//     "DeleteRecordIds": [""],
//     "Resources": [
//             {
//                 "Path": "",
//                 "Params": [
//                     {
//                         "Key": "",
//                         "Value": ""
//                     },
//                     {
//                         "Key": "",
//                         "Value": ""
//                     }
//                 ]
//             }
//         ]
// }