namespace ManagedFileService.Application.Features.Attachments.Commands.CreateAllowedApplication;

/// <summary>
/// Command to create a new AllowedApplication.
/// Takes the plain-text API key, which will be hashed by the handler.
/// </summary>
/// <param name="Name">The descriptive name of the application.</param>
/// <param name="PlainTextApiKey">The desired API Key in plain text (will be hashed).</param>
/// <param name="MaxFileSizeMegaBytes">Optional limit for individual file uploads in Megabytes.</param>
public record CreateAllowedApplicationCommand(
    string Name,
    string PlainTextApiKey,
    long? MaxFileSizeMegaBytes
) : IRequest<Guid>; // Returns the ID of the newly created application