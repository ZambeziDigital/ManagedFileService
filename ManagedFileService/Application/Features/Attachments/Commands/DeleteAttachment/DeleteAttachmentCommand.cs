namespace ManagedFileService.Application.Features.Attachments.Commands.DeleteAttachment;

public record DeleteAttachmentCommand(
    Guid AttachmentId
) : IRequest; // Returns Unit (void) upon successful completion