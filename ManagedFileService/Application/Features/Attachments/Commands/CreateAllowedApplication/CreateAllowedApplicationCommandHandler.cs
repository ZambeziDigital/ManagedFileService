global using ManagedFileService.Application.Common.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace ManagedFileService.Application.Features.Attachments.Commands.CreateAllowedApplication;

public class CreateAllowedApplicationCommandHandler : IRequestHandler<CreateAllowedApplicationCommand, Guid>
{
    private readonly IAllowedApplicationRepository _allowedAppRepository;
    private readonly ILogger<CreateAllowedApplicationCommandHandler> _logger;

    public CreateAllowedApplicationCommandHandler(
        IAllowedApplicationRepository allowedAppRepository,
        ILogger<CreateAllowedApplicationCommandHandler> logger)
    {
        _allowedAppRepository = allowedAppRepository ?? throw new ArgumentNullException(nameof(allowedAppRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Guid> Handle(CreateAllowedApplicationCommand request, CancellationToken cancellationToken)
    {
        // Basic Validation
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ValidationException("Application name cannot be empty.");
        }
        if (string.IsNullOrWhiteSpace(request.PlainTextApiKey))
        {
            throw new ValidationException("API Key cannot be empty.");
        }
        // Add other validation as needed (e.g., name uniqueness check if required)
        // var existing = await _allowedAppRepository.FindByNameAsync(request.Name, cancellationToken);
        // if (existing != null) throw new ValidationException($"Application with name '{request.Name}' already exists.");

        _logger.LogInformation("Attempting to create new AllowedApplication: {AppName}", request.Name);

        // **Securely hash the API Key using BCrypt**
        string apiKeyHash;
        try
        {
             // The work factor determines computational cost (higher is slower but more secure)
            apiKeyHash = BCrypt.Net.BCrypt.HashPassword(request.PlainTextApiKey);
             _logger.LogDebug("Generated API Key Hash for {AppName}", request.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to hash API Key for application {AppName}", request.Name);
            throw new InfrastructureException("Failed to secure API Key during application creation.", ex);
        }


        // Create the domain entity
        var newApplication = new AllowedApplication(
            name: request.Name,
            apiKeyHash: apiKeyHash, // Pass the HASH, not the plain text
            maxFileSizeMegaBytes: request.MaxFileSizeMegaBytes
        );

        // Persist using the repository
        // Note: Assumes IAllowedApplicationRepository has an AddAsync method
        try
        {
            // You need to add AddAsync to IAllowedApplicationRepository and implement it
            await _allowedAppRepository.AddAsync(newApplication, cancellationToken);
            _logger.LogInformation("Successfully created AllowedApplication {AppName} with ID {AppId}", newApplication.Name, newApplication.Id);
        }
        catch(DbUpdateException dbEx) // Catch potential DB constraint violations (e.g., unique index on Name if you add one)
        {
             _logger.LogError(dbEx, "Database error while creating AllowedApplication {AppName}", newApplication.Name);
             // Check inner exception for specifics (e.g., unique constraint violation)
             throw new InfrastructureException($"Failed to save new application '{newApplication.Name}' to the database.", dbEx);
        }
        catch (Exception ex)
        {
             _logger.LogError(ex, "Generic error while saving AllowedApplication {AppName}", newApplication.Name);
            throw; // Re-throw other unexpected errors
        }

        return newApplication.Id;
    }
}
