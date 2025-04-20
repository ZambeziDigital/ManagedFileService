namespace ManagedFileService.Application.Features.Attachments.Queries.GetAttachmentMetadata;

public record GetAttachmentMetadataQuery(
    Guid AttachmentId
) : IRequest<AttachmentMetadataDto?>; // Nullable DTO indicates potential not found scenario