using ManagedFileService.Domain.Entities;

namespace ManagedFileService.Application.Interfaces;

public interface IZipArchiveService
{
    /// <summary>
    /// Creates a ZIP archive containing the specified attachments
    /// </summary>
    /// <param name="attachments">The attachments to include in the archive</param>
    /// <param name="archiveName">The name for the archive (without extension)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Information about the created archive</returns>
    Task<ArchiveResult> CreateArchiveFromAttachmentsAsync(
        IEnumerable<Attachment> attachments,
        string archiveName,
        CancellationToken cancellationToken = default);
}

public class ArchiveResult
{
    public Guid ArchiveId { get; set; }
    public string ArchivePath { get; set; } = string.Empty;
    public long SizeInBytes { get; set; }
    public int FileCount { get; set; }
}
