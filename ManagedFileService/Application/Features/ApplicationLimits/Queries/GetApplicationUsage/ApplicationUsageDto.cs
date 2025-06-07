namespace ManagedFileService.Application.Features.ApplicationLimits.Queries.GetApplicationUsage;

public class ApplicationUsageDto
{
    public Guid ApplicationId { get; set; }
    public string ApplicationName { get; set; } = string.Empty;
    
    // Current usage
    public long CurrentStorageBytes { get; set; }
    public double CurrentStorageMegaBytes => Math.Round(CurrentStorageBytes / (1024.0 * 1024.0), 2);
    
    // Limits (if set)
    public long? MaxFileSizeBytes { get; set; }
    public double? MaxFileSizeMegaBytes => MaxFileSizeBytes.HasValue 
        ? Math.Round(MaxFileSizeBytes.Value / (1024.0 * 1024.0), 2) 
        : null;
    
    public long? MaxStorageBytes { get; set; }
    public double? MaxStorageMegaBytes => MaxStorageBytes.HasValue 
        ? Math.Round(MaxStorageBytes.Value / (1024.0 * 1024.0), 2) 
        : null;
    
    // Usage statistics
    public int TotalFiles { get; set; }
    public double StorageUsagePercentage => MaxStorageBytes.HasValue && MaxStorageBytes > 0
        ? Math.Round((CurrentStorageBytes / (double)MaxStorageBytes) * 100, 2)
        : 0;
}
