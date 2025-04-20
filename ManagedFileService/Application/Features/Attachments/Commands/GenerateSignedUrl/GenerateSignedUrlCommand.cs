namespace ManagedFileService.Application.Features.Attachments.Commands.GenerateSignedUrl;

public record GenerateSignedUrlCommand(
    Guid AttachmentId,
    int ExpiresInMinutes,
    string BasePublicUrl // Needed to construct the full URL
) : IRequest<GenerateSignedUrlResponse>;