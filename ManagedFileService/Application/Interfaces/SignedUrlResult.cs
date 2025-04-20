namespace ManagedFileService.Application.Interfaces;

public record SignedUrlResult(
    string Url,
    DateTimeOffset ExpiresAtUtc
);