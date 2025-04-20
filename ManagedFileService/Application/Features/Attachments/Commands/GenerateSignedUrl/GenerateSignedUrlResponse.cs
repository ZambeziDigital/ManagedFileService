namespace ManagedFileService.Application.Features.Attachments.Commands.GenerateSignedUrl;

public record GenerateSignedUrlResponse(
    string SignedUrl,
    DateTimeOffset ExpiresAtUtc
);