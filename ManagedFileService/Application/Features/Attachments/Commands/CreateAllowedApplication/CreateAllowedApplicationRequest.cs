namespace ManagedFileService.Application.Features.Attachments.Commands.CreateAllowedApplication;

/// <summary>
/// DTO representing the request body for creating an allowed application.
/// </summary>
public class CreateAllowedApplicationRequest
{
    public string Name { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty; // Corresponds to PlainTextApiKey
    public long? MaxFileSizeMegaBytes { get; set; }
}
