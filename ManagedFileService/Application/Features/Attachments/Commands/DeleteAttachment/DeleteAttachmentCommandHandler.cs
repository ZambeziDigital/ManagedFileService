using ManagedFileService.Application.Common.Exceptions;
using ManagedFileService.Application.Interfaces;

namespace ManagedFileService.Application.Features.Attachments.Commands.DeleteAttachment;


public class DeleteAttachmentCommandHandler : IRequestHandler<DeleteAttachmentCommand>
{
    private readonly IAttachmentRepository _attachmentRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly ICurrentRequestService _currentRequestService;
    private readonly ILogger<DeleteAttachmentCommandHandler> _logger;

    public DeleteAttachmentCommandHandler(
        IAttachmentRepository attachmentRepository,
        IFileStorageService fileStorageService,
        ICurrentRequestService currentRequestService,
        ILogger<DeleteAttachmentCommandHandler> logger)
    {
        _attachmentRepository = attachmentRepository ?? throw new ArgumentNullException(nameof(attachmentRepository));
        _fileStorageService = fileStorageService ?? throw new ArgumentNullException(nameof(fileStorageService));
        _currentRequestService = currentRequestService ?? throw new ArgumentNullException(nameof(currentRequestService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // MediatR V11 and later use 'ValueTask Handle', earlier use 'Task<Unit> Handle' or just 'Task Handle'
    public async Task Handle(DeleteAttachmentCommand request, CancellationToken cancellationToken)
    // If using older MediatR returning Unit: public async Task<Unit> Handle(...)
    // If using void Task: change IRequestHandler<DeleteAttachmentCommand> to IRequestHandler<DeleteAttachmentCommand, Unit> if needed
    {
        _logger.LogInformation("Attempting to delete attachment {AttachmentId}", request.AttachmentId);

        // 1. Retrieve Attachment Metadata
        var attachment = await _attachmentRepository.GetByIdAsync(request.AttachmentId, cancellationToken);

        if (attachment == null)
        {
            // Attachment not found. This isn't strictly an error if deletion is idempotent.
            // Log it and return successfully. Or throw NotFoundException if required by API contract.
            _logger.LogWarning("Attempted to delete non-existent attachment {AttachmentId}", request.AttachmentId);
             throw new NotFoundException(nameof(Attachment), request.AttachmentId);
            // return Unit.Value; // Or just return for Task Handle
        }

        // 2. Authorization Check
        var currentApplicationId = _currentRequestService.GetApplicationId();
        if (attachment.ApplicationId != currentApplicationId)
        {
            // Crucial security check: Prevent App A deleting App B's files
            _logger.LogError("Unauthorized delete attempt: App {RequestingAppId} tried to delete attachment {AttachmentId} owned by App {OwnerAppId}",
                currentApplicationId, attachment.Id, attachment.ApplicationId);
            // Throwing here prevents deletion. You might treat as NotFound depending on desired behaviour.
            throw new ForbiddenAccessException($"Application {currentApplicationId} is not authorized to delete attachment {request.AttachmentId}.");
        }

        // 3. Delete Physical File from Storage
        try
        {
            await _fileStorageService.DeleteAsync(attachment.StoredPath, cancellationToken);
            _logger.LogInformation("Successfully deleted stored file: {StoredPath} for attachment {AttachmentId}",
                attachment.StoredPath, attachment.Id);
        }
        catch (Exception ex)
        {
            // Log the error, but decide if you should proceed to delete the DB record.
            // - If you delete the DB record, you might have an orphaned file if deletion failed retrievably.
            // - If you DON'T delete the DB record, you have metadata pointing to a non-existent file (or a file that failed deletion).
            // Often, it's better to leave the DB record and have a background job cleanup process,
            // or retry the file deletion. For simplicity here, we log and proceed to delete the DB record,
            // acknowledging potential storage cleanup issues.
            _logger.LogError(ex, "Failed to delete file from storage: {StoredPath} for attachment {AttachmentId}. Proceeding with metadata deletion.",
                 attachment.StoredPath, attachment.Id);
            // Consider re-throwing or specific handling depending on storage service guarantees.
             // Rethrowing would stop the DB deletion, which might be safer. Let's rethrow for now.
            throw new InfrastructureException($"Failed to delete file '{attachment.StoredPath}' from storage.", ex);
        }

        // 4. Delete Metadata from Database
        try
        {
            await _attachmentRepository.DeleteAsync(attachment.Id, cancellationToken);
            _logger.LogInformation("Successfully deleted attachment metadata from DB: {AttachmentId}", attachment.Id);
        }
        catch (Exception ex)
        {
            // This is more serious - the file was deleted but the DB record wasn't.
            // Log critically. A background job might be needed to reconcile.
             _logger.LogCritical(ex, "CRITICAL: Failed to delete attachment metadata from DB after file was deleted. AttachmentId: {AttachmentId}, StoredPath: {StoredPath}",
                attachment.Id, attachment.StoredPath);
             // Re-throw the exception
            throw new InfrastructureException($"Failed to delete metadata for attachment '{attachment.Id}' from database after file was deleted.", ex);
        }

        // MediatR V11+: Methods returning Task complete implicitly
        // If using older MediatR: return Unit.Value;
    }
}