using MediatR;

namespace ManagedFileService.Application.Features.Attachments.Commands.UploadAttachment;

public record UploadAttachmentCommand(
    IFormFile File,
    string? UserId // Optional user identifier passed from client
) : IRequest<Guid>; // Returns the Id of the created attachment
