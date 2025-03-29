namespace ScrapperHttpFunction;

using Common.Configurations;
using Helpers;
using Models.DatabaseModels;
using Newtonsoft.Json;
using Octokit;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

public class UpdatePage
{
    private readonly ILogger _logger;
    private readonly GithubPersonalAccessTokenConfiguration _githubConfiguration;
    private readonly (string owner, string repoName, string filePath, string branch) _githubMetadata = 
        ("kowto-app", "kowto-app.github.io", "data/data.json", "main");

    public UpdatePage(ILoggerFactory loggerFactory, GithubPersonalAccessTokenConfiguration githubConfiguration)
    {
        _githubConfiguration = githubConfiguration;
        _logger = loggerFactory.CreateLogger<UpdatePage>();
    }

    [Function("UpdatePage")]
    public async Task Run([CosmosDBTrigger(
        databaseName: "ScrapperDB",
        containerName: "Vacancies",
        Connection = "CosmoConnectionString",
        LeaseContainerName = "leases",
        CreateLeaseContainerIfNotExists = true)] string triggeredInput)
    {
        _logger.LogInformation("Documents modified");

        try
        {
            if (string.IsNullOrEmpty(triggeredInput))
            {
                _logger.LogInformation("No updated data");
                return;
            }

            var incomeDataRaw = JsonConvert.DeserializeObject<List<JobInfoOutModel>>(triggeredInput);

            if (incomeDataRaw.Count == 0)
            {
                _logger.LogError("No updated data was serialized");
                return;
            }

            var incomeData = JobInfoValidationHelper.ValidateJobInfo(incomeDataRaw, _logger);
            if (incomeData.marker)
            {
                _logger.LogInformation("After validation not left any data to updating");
                return;
            }

            var githubClient = new GitHubClient(new ProductHeaderValue(_githubMetadata.owner));
            githubClient.Credentials = new Credentials(_githubConfiguration.PersonalAccessToken);

            var existRepositoryContent = 
                await githubClient.Repository.Content.GetAllContentsByRef(
                    _githubMetadata.owner,
                    _githubMetadata.repoName,
                    _githubMetadata. filePath,
                    _githubMetadata.branch);

            if (existRepositoryContent.Count == 0)
            {
                _logger.LogError("Repository content collection empty");
                return;
            }

            if (existRepositoryContent[0] is null)
            {
                _logger.LogInformation("No exist data found");
                return;
            }

            var existContent = existRepositoryContent[0].Content;
            var existJobsInfo = JsonConvert.DeserializeObject<List<JobInfoOutModel>>(existContent);
            existJobsInfo.AddRange(incomeData.collection);

            var content = JsonConvert.SerializeObject(existJobsInfo);

            var updatedData = await githubClient.Repository.Content.UpdateFile(
                _githubMetadata.owner,
                _githubMetadata.repoName,
                _githubMetadata. filePath,
                new UpdateFileRequest("update data", content, existRepositoryContent[0].Sha));

            if (string.IsNullOrEmpty(updatedData.Commit.Sha))
            {
                _logger.LogError("Something went wrong while updating data");
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize JSON data");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An error occurred while processing the request");
        }
    }
}