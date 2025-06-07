using System.IO.Compression;
using ManagedFileService.Application.Interfaces;
using ManagedFileService.Domain.Entities;

namespace ManagedFileService.Infrastructure.Services;

public class ZipArchiveService : IZipArchiveService
{
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<ZipArchiveService> _logger;

    public ZipArchiveService(IFileStorageService fileStorageService, ILogger<ZipArchiveService> logger)
    {
        _fileStorageService = fileStorageService ?? throw new ArgumentNullException(nameof(fileStorageService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Stream> CreateZipArchiveFromAttachments(IEnumerable<Attachment> attachments, CancellationToken cancellationToken = default)
    {
        var memoryStream = new MemoryStream();
        
        try
        {
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                foreach (var attachment in attachments)
                {
                    try
                    {
                        // Get file stream from storage
                        var fileStream = await _fileStorageService.GetAsync(attachment.StoredPath, cancellationToken);
                        if (fileStream == null)
                        {
                            _logger.LogWarning("File not found in storage for attachment {AttachmentId}", attachment.Id);
                            continue;
                        }

                        // Create entry in ZIP with original filename
                        var entryName = attachment.OriginalFileName;
                        var entry = archive.CreateEntry(EnsureUniqueEntryName(archive, entryName), CompressionLevel.Optimal);

                        // Write file content to the ZIP entry
                        using (var entryStream = entry.Open())
                        using (fileStream)
                        {
                            await fileStream.CopyToAsync(entryStream, cancellationToken);
                        }

                        _logger.LogDebug("Added file {FileName} to ZIP archive", entryName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error adding attachment {AttachmentId} to ZIP archive", attachment.Id);
                        // Continue with other files instead of failing the whole operation
                    }
                }
            }

            // Reset position to start of stream for reading
            memoryStream.Position = 0;
            return memoryStream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating ZIP archive");
            memoryStream.Dispose();
            throw;
        }
    }

    private string EnsureUniqueEntryName(ZipArchive archive, string desiredName)
    {
        // Check if the name already exists in the archive
        if (archive.Entries.Any(e => e.FullName.Equals(desiredName, StringComparison.OrdinalIgnoreCase)))
        {
            // Add a timestamp to make it unique
            var fileName = Path.GetFileNameWithoutExtension(desiredName);
            var extension = Path.GetExtension(desiredName);
            return $"{fileName}_{DateTime.UtcNow:yyyyMMddHHmmss}{extension}";
        }

        return desiredName;
    }

    public Task<ArchiveResult> CreateArchiveFromAttachmentsAsync(IEnumerable<Attachment> attachments, string archiveName,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
