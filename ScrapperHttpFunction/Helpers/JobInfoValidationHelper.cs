namespace ScrapperHttpFunction.Helpers;

using Microsoft.Extensions.Logging;
using Models.DatabaseModels;
using Newtonsoft.Json;

public class JobInfoValidationHelper
{
    public static (List<JobInfoOutModel> collection, bool marker) ValidateJobInfo(IEnumerable<JobInfoOutModel> jobInfos, ILogger logger)
    {
        var result = new List<JobInfoOutModel>();
        bool marker = false;

        foreach (var item in jobInfos)
        {
            if (!string.IsNullOrEmpty(item.Id) && 
                item.TimestampUnix != 0 )
            {
                result.Add(item);
            }
            else
            {
                marker = true;
                logger.LogWarning($"Job info validation failed for: {JsonConvert.SerializeObject(item)}");
            }
        }

        return (result, marker);
    }
}