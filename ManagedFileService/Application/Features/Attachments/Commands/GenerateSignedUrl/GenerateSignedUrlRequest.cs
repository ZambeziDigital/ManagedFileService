namespace ManagedFileService.Application.Features.Attachments.Commands.GenerateSignedUrl;

public class GenerateSignedUrlRequest
{
    public int ExpiresInMinutes { get; set; } = 5; // Default to 5 minutes
}
