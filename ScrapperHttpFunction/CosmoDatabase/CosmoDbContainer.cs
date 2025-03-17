namespace ScrapperHttpFunction.CosmoDatabase;

using System.Collections.ObjectModel;
using System.Net;
using Entities;
using Microsoft.Azure.Cosmos;

public class CosmoDbContainer
{
    private bool _initialized;
    // The Cosmos client instance
    private CosmosClient _cosmosClient;

    // The database we will create
    private Database _database;

    // The container we will create.
    private Dictionary<string, Container> _container = new ();

    // The name of the database and container we will create
    private string databaseId = "ScrapperDB";

    private static Dictionary<string, string> _containerIds = new ()
    {
        { typeof(JobInfo).AssemblyQualifiedName, "Vacancies"},
        { typeof(Resource).AssemblyQualifiedName, "Resources"},
        { typeof(RequestsId).AssemblyQualifiedName, "RequestsId"},
    };

    public CosmoDbContainer(string connectionString)
    {
        _cosmosClient = new CosmosClient(
            connectionString: connectionString, 
            clientOptions: new CosmosClientOptions { ApplicationName = "Scrapper", AllowBulkExecution = true });
    }

    public async Task Initialize()
    {
        if (!_initialized)
        {
            // Create a new database
            _database = await _cosmosClient.CreateDatabaseIfNotExistsAsync(databaseId);

            // Create a new container
            foreach (var containerId in _containerIds)
            {
                var response = await _database.CreateContainerIfNotExistsAsync(containerId.Value, "/id");
                if (response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.OK)
                {
                    _container.TryAdd(containerId.Key, response);
                }
            }

            _initialized = true;
        }
    }

    public IReadOnlyDictionary<string, Container> GetContainer =>  new ReadOnlyDictionary<string, Container>(_container);
}