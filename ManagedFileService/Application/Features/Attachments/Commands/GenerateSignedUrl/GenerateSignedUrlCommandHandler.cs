using ManagedFileService.Application.Interfaces;

namespace ManagedFileService.Application.Features.Attachments.Commands.GenerateSignedUrl;


public class GenerateSignedUrlCommandHandler : IRequestHandler<GenerateSignedUrlCommand, GenerateSignedUrlResponse>
{
    private readonly IAttachmentRepository _attachmentRepository;
    private readonly ISignedUrlService _signedUrlService;
    private readonly ICurrentRequestService _currentRequestService; // To verify ownership
    private readonly ILogger<GenerateSignedUrlCommandHandler> _logger;

    public GenerateSignedUrlCommandHandler(
        IAttachmentRepository attachmentRepository,
        ISignedUrlService signedUrlService,
        ICurrentRequestService currentRequestService,
        ILogger<GenerateSignedUrlCommandHandler> logger)
    {
        _attachmentRepository = attachmentRepository;
        _signedUrlService = signedUrlService;
        _currentRequestService = currentRequestService;
        _logger = logger;
    }

    public async Task<GenerateSignedUrlResponse> Handle(GenerateSignedUrlCommand request, CancellationToken cancellationToken)
    {
        // 1. Verify Attachment Exists
        var attachment = await _attachmentRepository.GetByIdAsync(request.AttachmentId, cancellationToken);
        if (attachment == null)
        {
            throw new NotFoundException(nameof(Domain.Entities.Attachment), request.AttachmentId);
        }

        // 2. Verify Ownership (Authorization)
        var currentApplicationId = _currentRequestService.GetApplicationId();
        if (attachment.ApplicationId != currentApplicationId)
        {
            _logger.LogWarning("Unauthorized attempt to generate signed URL for attachment {AttachmentId} by application {AppId}. Owner is {OwnerAppId}.",
                request.AttachmentId, currentApplicationId, attachment.ApplicationId);
            // Throw Forbidden or NotFound depending on desired behavior for security. NotFound hides existence.
            throw new NotFoundException(nameof(Domain.Entities.Attachment), request.AttachmentId);
             // throw new ForbiddenAccessException($"Application {currentApplicationId} is not authorized for attachment {request.AttachmentId}.");
        }

        // 3. Generate the Signed URL using the service
        SignedUrlResult generationResult; // <-- Use the new result type
        try
        {
            generationResult = _signedUrlService.GenerateSignedUrl( // <-- Call the updated method
                request.AttachmentId,
                request.ExpiresInMinutes,
                request.BasePublicUrl);
        }
        catch(ArgumentException argEx)
        {
             // Catch errors like expiry too long from the service
             _logger.LogWarning(argEx, "Invalid argument while generating signed URL for {AttachmentId}.", request.AttachmentId);
             throw new ValidationException(argEx.Message); // Convert to application validation exception
        }
        catch (Exception ex)
        {
             _logger.LogError(ex, "Failed to generate signed URL for attachment {AttachmentId}.", request.AttachmentId);
            throw new InfrastructureException("Could not generate the temporary access URL.", ex);
        }


        // 4. Calculate expiry for the response DTO
        return new GenerateSignedUrlResponse(
            SignedUrl: generationResult.Url,
            ExpiresAtUtc: generationResult.ExpiresAtUtc);

        // return new GenerateSignedUrlResponse(signedUrl, actualExpiry);
    }
}