namespace ManagedFileService.Application.Features.System.Queries.GetSystemStatus;

public class SystemStatusDto
{
    public bool IsHealthy { get; set; }
    public string Version { get; set; } = string.Empty;
    public DateTime ServerTime { get; set; }
    public Dictionary<string, bool> Components { get; set; } = new();
    public long TotalStorageUsedBytes { get; set; }
    public int TotalApplications { get; set; }
    public int TotalAttachments { get; set; }
}
