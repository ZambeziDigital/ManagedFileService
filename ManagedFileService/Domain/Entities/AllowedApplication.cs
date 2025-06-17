namespace ManagedFileService.Domain.Entities;

public class AllowedApplication
{
    // Primary identity
    public Guid Id { get; private set; }

    // Application metadata
    public string Name { get; private set; } = string.Empty;
    public string ApiKeyHash { get; private set; } = string.Empty; // Never store plain text

    // Application limits/settings
    public long? MaxFileSizeBytes { get; private set; } // If null, no limit (beyond service max)

    // Total storage limit for this application
    public long? MaxStorageBytes { get; private set; } // If null, no limit (beyond service max)

    // Admin flag to identify applications with management privileges
    public bool IsAdmin { get; private set; } = false;

    // Required by EF Core
    private AllowedApplication() { }

    public AllowedApplication(string name, string apiKeyHash, long? maxFileSizeBytes = null,
        bool isAdmin = false, long? maxStorageBytes = null)
    {
        // Generate a new ID
        Id = Guid.NewGuid();

        // Validate and set
        SetName(name);
        SetApiKeyHash(apiKeyHash);
        SetMaxFileSize(maxFileSizeBytes);
        SetMaxStorageSize(maxStorageBytes);
        SetAdminStatus(isAdmin);
    }

    public void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Application name cannot be empty.", nameof(name));

        Name = name;
    }

    public void SetApiKeyHash(string apiKeyHash)
    {
        if (string.IsNullOrWhiteSpace(apiKeyHash))
            throw new ArgumentException("API Key hash cannot be empty.", nameof(apiKeyHash));

        ApiKeyHash = apiKeyHash;
    }

    public void SetMaxFileSize(long? maxFileSizeBytes)
    {
        // Validate if provided
        if (maxFileSizeBytes.HasValue && maxFileSizeBytes.Value <= 0)
            throw new ArgumentException("Max file size must be positive.", nameof(maxFileSizeBytes));

        MaxFileSizeBytes = maxFileSizeBytes;
    }

    public void SetMaxStorageSize(long? maxStorageBytes)
    {
        // Validate if provided
        if (maxStorageBytes.HasValue && maxStorageBytes.Value <= 0)
            throw new ArgumentException("Max storage size must be positive.", nameof(maxStorageBytes));

        MaxStorageBytes = maxStorageBytes;
    }

    public void SetAdminStatus(bool isAdmin)
    {
        IsAdmin = isAdmin;
    }
    
    public void UpdateApiKey(string newApiKeyHash)
{
    SetApiKeyHash(newApiKeyHash);
    // Could add UpdatedAt timestamp here if you have that field
}
}