global using ManagedFileService.Domain.Interfaces;
global using MediatR;
using ManagedFileService.Application.Interfaces;

namespace ManagedFileService.Application.Features.Attachments.Commands.UploadAttachment;

public class UploadAttachmentCommandHandler : IRequestHandler<UploadAttachmentCommand, Guid>
{
    private readonly IAttachmentRepository _attachmentRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly ICurrentRequestService _currentRequestService;
    private readonly IAllowedApplicationRepository _allowedAppRepository; // Needed for validation

    public UploadAttachmentCommandHandler(
        IAttachmentRepository attachmentRepository,
        IFileStorageService fileStorageService,
        ICurrentRequestService currentRequestService,
        IAllowedApplicationRepository allowedAppRepository)
    {
        _attachmentRepository = attachmentRepository;
        _fileStorageService = fileStorageService;
        _currentRequestService = currentRequestService;
        _allowedAppRepository = allowedAppRepository;
    }

    public async Task<Guid> Handle(UploadAttachmentCommand request, CancellationToken cancellationToken)
    {
        var applicationId = _currentRequestService.GetApplicationId();
        var application = await _allowedAppRepository.GetByIdAsync(applicationId, cancellationToken); // Fetch app details for validation

        if (application == null)
        {
            // This shouldn't happen if auth is working, but good practice
            throw new UnauthorizedAccessException("Application not found.");
        }

        // --- Validation ---
        // Size Validation
        long fileSizeLimitBytes = application.MaxFileSizeBytes ?? long.MaxValue;
        if (request.File.Length > fileSizeLimitBytes)
        {
            throw new ArgumentException($"File size exceeds the allowed limit of {application.MaxFileSizeBytes / (1024 * 1024)} MB for this application.");
            // Consider a custom ValidationException
        }

        // Storage Limit Validation
        if (application.MaxStorageBytes.HasValue)
        {
            // Get current usage
            var currentUsage = await _attachmentRepository.GetStorageBytesForApplicationAsync(applicationId, cancellationToken);
            
            // Check if the new file would exceed the limit
            if (currentUsage + request.File.Length > application.MaxStorageBytes.Value)
            {
                throw new ArgumentException($"This upload would exceed your storage limit of {application.MaxStorageBytes / (1024 * 1024)} MB. " +
                                           $"Current usage: {currentUsage / (1024 * 1024)} MB.");
            }
        }
        // Potential Content-Type Validation based on app config could go here

        // --- Processing ---
        var uniqueFileName = _fileStorageService.GenerateUniqueFileName(request.File.FileName);
        string storedPath;

        await using (var stream = request.File.OpenReadStream())
        {
            storedPath = await _fileStorageService.SaveAsync(stream, uniqueFileName, request.File.ContentType, cancellationToken);
        }

        var attachment = new Attachment(
            fileName: uniqueFileName, // Use the unique name
            originalFileName: request.File.FileName,
            contentType: request.File.ContentType,
            sizeBytes: request.File.Length,
            storedPath: storedPath,
            applicationId: applicationId,
            userId: request.UserId);

        await _attachmentRepository.AddAsync(attachment, cancellationToken);
        // Note: Ideally, saving the file and saving the DB record should be atomic.
        // Consider patterns like Outbox or Transactional Outbox for robustness in production.
        // For simplicity here, we assume success or handle potential cleanup in catch block/failure scenarios.

        return attachment.Id;
    }
}