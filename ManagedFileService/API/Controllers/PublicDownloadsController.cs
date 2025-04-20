using ManagedFileService.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ManagedFileService.API.Controllers;


[ApiController]
[Route("api/[controller]")]
[AllowAnonymous] // IMPORTANT: This endpoint MUST be publicly accessible
public class PublicDownloadsController : ControllerBase
{
    private readonly ISignedUrlService _signedUrlService;
    private readonly IAttachmentRepository _attachmentRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<PublicDownloadsController> _logger;

    public PublicDownloadsController(
        ISignedUrlService signedUrlService,
        IAttachmentRepository attachmentRepository,
        IFileStorageService fileStorageService,
        ILogger<PublicDownloadsController> logger)
    {
        _signedUrlService = signedUrlService;
        _attachmentRepository = attachmentRepository;
        _fileStorageService = fileStorageService;
        _logger = logger;
    }

    /// <summary>
    /// Downloads an attachment using a temporary signed URL.
    /// </summary>
    /// <param name="id">Attachment ID (from query string).</param>
    /// <param name="expires">Expiry timestamp (Unix epoch seconds) (from query string).</param>
    /// <param name="sig">Signature (from query string).</param>
    /// <returns>File stream or an error response (401/404).</returns>
    [HttpGet("download", Name = "DownloadPublicAttachment")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)] // Invalid signature or expired
    [ProducesResponseType(StatusCodes.Status404NotFound)] // Attachment not found
    public async Task<IActionResult> Download(
        [FromQuery] Guid id,
        [FromQuery] long expires,
        [FromQuery] string sig,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received public download request for Attachment {AttachmentId}, Expires: {Expires}, Sig: {Signature}",
            id, expires, sig);

        // 1. Validate the Signed URL (Signature & Expiry)
        if (!_signedUrlService.ValidateSignedUrl(id, expires, sig))
        {
            // Logged within ValidateSignedUrl
            return Unauthorized("Invalid or expired download link."); // Return 401
        }

        _logger.LogDebug("Signed URL is valid for Attachment {AttachmentId}. Proceeding to fetch.", id);


        // 2. Retrieve Attachment Metadata
        var attachment = await _attachmentRepository.GetByIdAsync(id, cancellationToken);
        if (attachment == null)
        {
            _logger.LogWarning("Attachment {AttachmentId} not found after successful signed URL validation.", id);
             // Use NotFound even if the link was technically valid, as the resource is gone
            return NotFound("Attachment not found.");
        }

        // 3. Get the File Stream from storage
        Stream? fileStream = null;
        try
        {
             fileStream = await _fileStorageService.GetAsync(attachment.StoredPath, cancellationToken);
        }
        catch(Exception ex)
        {
             // Catch specific storage exceptions if possible
             _logger.LogError(ex, "Error retrieving file from storage for Attachment {AttachmentId}, Path: {StoredPath}", id, attachment.StoredPath);
             return StatusCode(StatusCodes.Status500InternalServerError, "Error accessing file storage."); // Don't leak details
        }


        if (fileStream == null)
        {
             _logger.LogError("File data not found in storage for Attachment {AttachmentId}, Path: {StoredPath}, although metadata exists.", id, attachment.StoredPath);
            // Metadata exists but file is missing from storage
            return NotFound("File data not found.");
        }

        _logger.LogInformation("Streaming file {OriginalFileName} ({ContentType}) for Attachment {AttachmentId}",
             attachment.OriginalFileName, attachment.ContentType, id);

        // 4. Return the File Stream - IMPORTANT: Allow synchronous IO for FileStreamResult potentially
        // May need builder.WebHost.ConfigureKestrel(serverOptions => { serverOptions.AllowSynchronousIO = true; }); or similar if issues arise.
        return File(fileStream, attachment.ContentType, attachment.OriginalFileName); // Provides original filename for download prompt
    }
}