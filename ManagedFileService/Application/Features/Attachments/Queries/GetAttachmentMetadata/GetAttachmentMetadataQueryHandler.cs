using ManagedFileService.Application.Interfaces;

namespace ManagedFileService.Application.Features.Attachments.Queries.GetAttachmentMetadata;

public class GetAttachmentMetadataQueryHandler : IRequestHandler<GetAttachmentMetadataQuery, AttachmentMetadataDto?>
{
    private readonly IAttachmentRepository _attachmentRepository;
    private readonly ICurrentRequestService _currentRequestService;
    private readonly ILogger<GetAttachmentMetadataQueryHandler> _logger; // Optional

    public GetAttachmentMetadataQueryHandler(
        IAttachmentRepository attachmentRepository,
        ICurrentRequestService currentRequestService,
        ILogger<GetAttachmentMetadataQueryHandler> logger) // Inject logger if needed
    {
        _attachmentRepository = attachmentRepository;
        _currentRequestService = currentRequestService;
        _logger = logger;
    }

    public async Task<AttachmentMetadataDto?> Handle(GetAttachmentMetadataQuery request, CancellationToken cancellationToken)
    {
        var attachment = await _attachmentRepository.GetByIdAsync(request.AttachmentId, cancellationToken);

        if (attachment == null)
        {
            // Attachment simply doesn't exist
            _logger.LogInformation("Attachment metadata requested for non-existent ID: {AttachmentId}", request.AttachmentId);
            return null; // Return null as per IRequest<AttachmentMetadataDto?> definition
        }

        // --- Authorization Check ---
        // Ensure the application making the request is the one that uploaded the file.
        var currentApplicationId = _currentRequestService.GetApplicationId();
        if (attachment.ApplicationId != currentApplicationId)
        {
            // Log the unauthorized access attempt. Do NOT return details about the file.
            _logger.LogWarning("Unauthorized attempt to access attachment metadata. Requesting AppId: {RequestingAppId}, Attachment Owner AppId: {AttachmentAppId}, AttachmentId: {AttachmentId}",
                currentApplicationId, attachment.ApplicationId, attachment.Id);
            return null; // Treat as "Not Found" from the caller's perspective
                         // Alternatively, you could throw an UnauthorizedAccessException if your API contract prefers throwing exceptions
                         // throw new UnauthorizedAccessException($"Application {currentApplicationId} is not authorized to access attachment {request.AttachmentId}.");
        }

        // --- Mapping ---
        // Map the domain entity to the DTO
        var metadataDto = new AttachmentMetadataDto(
            Id: attachment.Id,
            OriginalFileName: attachment.OriginalFileName,
            ContentType: attachment.ContentType,
            SizeBytes: attachment.SizeBytes,
            UploadedAtUtc: attachment.UploadedAtUtc,
            UserId: attachment.UserId
        );

        return metadataDto;
    }
}