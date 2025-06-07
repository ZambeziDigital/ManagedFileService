namespace ManagedFileService.Application.Features.Files.Commands.CreateArchive;

public class CreateArchiveResponse
{
    public Guid ArchiveId { get; set; }
    public string DownloadUrl { get; set; } = string.Empty;
    public int FileCount { get; set; }
    public long TotalSizeBytes { get; set; }
}
