namespace ManagedFileService.Domain.Entities;

public class Attachment
{
    public Guid Id { get; private set; }
    public string FileName { get; private set; } // Sanitized/Unique name for storage
    public string OriginalFileName { get; private set; }
    public string ContentType { get; private set; }
    public long SizeBytes { get; private set; }
    public string StoredPath { get; private set; } // Relative path or identifier in storage
    public DateTime UploadedAtUtc { get; private set; }
    public Guid ApplicationId { get; private set; } // FK to AllowedApplication
    public string? UserId { get; private set; } // Optional user identifier from calling app

    // Private constructor for EF Core
    private Attachment() { }

    public Attachment(
        string fileName,
        string originalFileName,
        string contentType,
        long sizeBytes,
        string storedPath,
        Guid applicationId,
        string? userId)
    {
        Id = Guid.NewGuid();
        FileName = fileName; // Ensure this is safe/unique
        OriginalFileName = originalFileName;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        StoredPath = storedPath;
        ApplicationId = applicationId;
        UserId = userId;
        UploadedAtUtc = DateTime.UtcNow;
    }
}