namespace ScrapperHttpFunction.CosmoDatabase;

using Microsoft.Azure.Cosmos;

public class CosmoDbContainer
{
    private bool _initialized;
    // The Cosmos client instance
    private CosmosClient _cosmosClient;

    // The database we will create
    private Database _database;

    // The container we will create.
    private Container _container;
    
    // The name of the database and container we will create
    private string databaseId = "ScrapperDB";
    private string containerId = "Vacancies";

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
            _container = await _database.CreateContainerIfNotExistsAsync(containerId, "/id");
            _initialized = true;
        }
    }
    
    public Container GetContainer => _container;
}