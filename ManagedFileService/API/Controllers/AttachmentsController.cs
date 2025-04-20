using ManagedFileService.Application.Features.Attachments.Commands.DeleteAttachment;
using ManagedFileService.Application.Features.Attachments.Commands.GenerateSignedUrl;
using ManagedFileService.Application.Features.Attachments.Commands.UploadAttachment;
using ManagedFileService.Application.Features.Attachments.Queries.GetAttachmentMetadata;
using ManagedFileService.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ManagedFileService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
// No [Authorize] here if ApiKeyAuthMiddleware handles it globally
public class AttachmentsController : ControllerBase
{
    private readonly ISender _mediator; // Use ISender in .NET 8 controllers
    private readonly IAttachmentRepository _attachmentRepository; // Needed for direct file access
    private readonly IFileStorageService _fileStorageService; // Needed for direct file access

    public AttachmentsController(ISender mediator, IAttachmentRepository attachmentRepository, IFileStorageService fileStorageService)
    {
        _mediator = mediator;
        _attachmentRepository = attachmentRepository;
        _fileStorageService = fileStorageService;
    }

    /// <summary>
    /// Uploads a new attachment. Requires X-Api-Key header.
    /// </summary>
    [HttpPost(Name = "UploadAttachment")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    // Add request size limit attribute if needed: [RequestSizeLimit(100_000_000)] // 100 MB example
    public async Task<IActionResult> Upload(IFormFile file, [FromForm] string? userId) // Bind optional userId from form data
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }

        var command = new UploadAttachmentCommand(file, userId);
        var attachmentId = await _mediator.Send(command);

        // Return 201 Created with the location of the metadata endpoint
        return CreatedAtAction(nameof(GetMetadata), new { id = attachmentId }, attachmentId);
    }

    /// <summary>
    /// Gets metadata for a specific attachment. Requires X-Api-Key header.
    /// </summary>
    [HttpGet("{id:guid}/metadata", Name = "GetAttachmentMetadata")]
    [ProducesResponseType(typeof(AttachmentMetadataDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AttachmentMetadataDto>> GetMetadata(Guid id)
    {
        var query = new GetAttachmentMetadataQuery(id);
        var metadata = await _mediator.Send(query);
        return metadata == null ? NotFound() : Ok(metadata);
    }

    /// <summary>
    /// Downloads the content of a specific attachment. Requires X-Api-Key header.
    /// </summary>
    [HttpGet("{id:guid}", Name = "DownloadAttachment")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Download(Guid id)
    {
        // Optimization: Could create a dedicated Application layer query/handler for this,
        // but direct repo/storage access is also viable in the controller for simple downloads.
        var attachment = await _attachmentRepository.GetByIdAsync(id);

        if (attachment == null)
        {
            return NotFound();
        }

        // Security Check: Ensure the requesting app owns this attachment?
        // var currentApp = HttpContext.Items["AllowedApplication"] as AllowedApplication;
        // if (currentApp == null || attachment.ApplicationId != currentApp.Id) { return Forbid(); }


        var fileStream = await _fileStorageService.GetAsync(attachment.StoredPath);

        if (fileStream == null)
        {
            // Log error: Metadata exists but file is missing
            return NotFound("File data not found.");
        }

        // Return the file stream
        return File(fileStream, attachment.ContentType, attachment.OriginalFileName); // Enables download with original name
    }

    /// <summary>
    /// Deletes an attachment (metadata and file). Requires X-Api-Key header.
    /// </summary>
    [HttpDelete("{id:guid}", Name = "DeleteAttachment")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var command = new DeleteAttachmentCommand(id);
        await _mediator.Send(command); // Handler should deal with NotFound case
        return NoContent();
    }
    
    
    /// <summary>
    /// Generates a temporary, pre-signed URL to download an attachment directly
    /// without requiring API key authentication. Requires standard API Key Auth to generate.
    /// </summary>
    /// <param name="id">The ID of the attachment to generate the URL for.</param>
    /// <param name="request">Specifies the desired validity duration for the URL.</param>
    /// <returns>The generated signed URL and its expiration time.</returns>
    [HttpPost("{id:guid}/generatesignedurl", Name = "GenerateSignedUrl")]
    [ProducesResponseType(typeof(GenerateSignedUrlResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)] // Invalid input (e.g., duration)
    [ProducesResponseType(StatusCodes.Status401Unauthorized)] // If API key is missing/invalid
    [ProducesResponseType(StatusCodes.Status403Forbidden)] // If app doesn't own the attachment
    [ProducesResponseType(StatusCodes.Status404NotFound)] // Attachment not found
    public async Task<ActionResult<GenerateSignedUrlResponse>> GenerateSignedUrl(Guid id, [FromBody] GenerateSignedUrlRequest request)
    {
        // Need to construct the base URL for the public download endpoint.
        // It's best practice not to hardcode this. Get it from request context.
        var scheme = Request.Scheme; // http or https
        var host = Request.Host.Value; // e.g., localhost:5001 or your-service.com
        // Construct the base URL dynamically based on where the *request* is coming from
        // Ensure the route to PublicDownloadsController.Download is correct
        var basePublicUrl = $"{scheme}://{host}/api/publicdownloads/download"; // Adjust path if necessary


        var command = new GenerateSignedUrlCommand(id, request.ExpiresInMinutes, basePublicUrl);

        try
        {
            var response = await _mediator.Send(command);
            return Ok(response);
        }
        catch(NotFoundException)
        {
            return NotFound();
        }
        catch (ForbiddenAccessException ex)
        {
             return Forbid(ex.Message); // Or return NotFound based on policy
        }
        catch (ValidationException ex)
        {
             return BadRequest(new { Message = ex.Message });
        }
        catch (InfrastructureException ex)
        {
            // Logged in handler
            return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Failed to generate signed URL." });
        }
        // Other unexpected exceptions handled globally
    }
}
