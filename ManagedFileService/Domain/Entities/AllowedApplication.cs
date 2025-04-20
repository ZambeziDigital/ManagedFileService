namespace ManagedFileService.Domain.Entities;

public class AllowedApplication
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string ApiKeyHash { get; private set; } // Store hash, not the key itself
    public long? MaxFileSizeMegaBytes { get; private set; } // Nullable for no limit
    // Add other config: MaxTotalStorageMB, AllowedContentTypes etc.

    private AllowedApplication() { }

    public AllowedApplication(string name, string apiKeyHash, long? maxFileSizeMegaBytes)
    {
        Id = Guid.NewGuid();
        Name = name;
        ApiKeyHash = apiKeyHash; // Ensure hash is generated before passing here
        MaxFileSizeMegaBytes = maxFileSizeMegaBytes;
    }
}