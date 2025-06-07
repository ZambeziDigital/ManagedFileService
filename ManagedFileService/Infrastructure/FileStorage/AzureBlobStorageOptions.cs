namespace ManagedFileService.Infrastructure.FileStorage;

public class AzureBlobStorageOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string ContainerName { get; set; } = "attachments";
}
