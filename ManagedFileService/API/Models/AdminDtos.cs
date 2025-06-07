namespace ManagedFileService.API.Models;

public class DashboardStatsDto
{
    public int TotalAttachments { get; set; }
    public long TotalStorageBytes { get; set; }
    public int TotalApplications { get; set; }
    public List<ApplicationStatsDto> ApplicationStats { get; set; } = new();
}

public class ApplicationStatsDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public long? MaxFileSizeBytes { get; set; }
    public long TotalStorageBytes { get; set; }
}

public class ApplicationDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public long? MaxFileSizeBytes { get; set; }
}

public class ApplicationDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public long? MaxFileSizeBytes { get; set; }
    public long TotalStorageBytes { get; set; }
    public List<AttachmentMetadataDto> RecentAttachments { get; set; } = new();
}

public class CreateApplicationRequest
{
    public string Name { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public long? MaxFileSizeMegaBytes { get; set; }
    public bool IsAdmin { get; set; }
}

public class AttachmentListItemDto
{
    public Guid Id { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTime UploadedAtUtc { get; set; }
    public string? UserId { get; set; }
    public Guid ApplicationId { get; set; }
    public string ApplicationName { get; set; } = string.Empty;
}

public class PaginatedList<T> where T : class
{
    public List<T> Items { get; set; } = new();
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}

public class StorageSummaryDto
{
    public long TotalStorageBytes { get; set; }
    public double TotalStorageMegabytes { get; set; }
    public List<ApplicationStorageDto> ApplicationStorage { get; set; } = new();
}

public class ApplicationStorageDto
{
    public Guid ApplicationId { get; set; }
    public string ApplicationName { get; set; } = string.Empty;
    public long StorageBytes { get; set; }
    public double StorageMegabytes { get; set; }
}
