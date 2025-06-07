namespace ManagedFileService.Application.Features.Attachments.Commands.CreateAllowedApplication;

/// <summary>
/// Command to create a new AllowedApplication.
/// The command contains the plain text API key, which will be hashed before storage.
/// </summary>
public record CreateAllowedApplicationCommand(
    string Name,
    string ApiKey,
    long? MaxFileSizeMegaBytes = null,
    bool IsAdmin = false) : IRequest<Guid>;