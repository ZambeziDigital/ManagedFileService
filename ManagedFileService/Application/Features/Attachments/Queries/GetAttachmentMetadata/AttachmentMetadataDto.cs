namespace ManagedFileService.Application.Features.Attachments.Queries.GetAttachmentMetadata;

public record AttachmentMetadataDto(
    Guid Id,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    DateTime UploadedAtUtc,
    string? UserId);