namespace ScrapperHttpFunction.Common.Configurations;

public class AzureBlobContainerConfiguration
{
    public string StorageContainerUrl { get; init; }
    public string StorageAccountName { get; init; }
    public string StorageAccountKey { get; init; }
}