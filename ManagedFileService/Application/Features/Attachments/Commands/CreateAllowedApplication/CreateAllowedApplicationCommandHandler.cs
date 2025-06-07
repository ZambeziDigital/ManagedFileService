global using ManagedFileService.Application.Common.Exceptions;
using Microsoft.EntityFrameworkCore;
using ManagedFileService.Domain.Interfaces;

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
        // 1. Basic input validation
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ValidationException("Application name cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(request.ApiKey))
        {
            throw new ValidationException("API Key cannot be empty.");
        }
        
        if (request.ApiKey.Length < 16) // Minimum recommended length for a decent API key
        {
            throw new ValidationException("API Key must be at least 16 characters long for security.");
        }

        // 2. Convert MB to bytes if MaxFileSizeMegaBytes is provided
        long? maxFileSizeBytes = null;
        if (request.MaxFileSizeMegaBytes.HasValue)
        {
            // Quick validation
            if (request.MaxFileSizeMegaBytes.Value <= 0)
            {
                throw new ValidationException("Max file size must be positive.");
            }
            
            // Convert MB to bytes (1 MB = 1,048,576 bytes)
            maxFileSizeBytes = request.MaxFileSizeMegaBytes.Value * 1_048_576;
        }

        // 3. Generate a secure hash of the API Key
        string apiKeyHash;
        try
        {
            // BCrypt.Net.BCrypt automatically handles salt generation and inclusion in the hash string
            apiKeyHash = BCrypt.Net.BCrypt.HashPassword(request.ApiKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to hash API key during application creation.");
            throw new InfrastructureException("Error occurred while processing security credentials.", ex);
        }

        // 4. Create entity with the hash (NOT the plain text key)
        var newApplication = new AllowedApplication(
            name: request.Name,
            apiKeyHash: apiKeyHash, // Pass the HASH, not the plain text
            maxFileSizeBytes: maxFileSizeBytes, // FIX: Use the converted value instead of original
            isAdmin: request.IsAdmin);

        // 5. Save to repository
        try
        {
            await _allowedAppRepository.AddAsync(newApplication, cancellationToken);
            _logger.LogInformation("Successfully created AllowedApplication {AppName} with ID {AppId}", newApplication.Name, newApplication.Id);
            
            // Add a very clear log message that explains what to use as the API key
            _logger.LogWarning(
                "IMPORTANT: For application {AppName} ({AppId}), you must use the ORIGINAL API KEY '{ApiKeyHint}...' for authentication, NOT the Application ID!", 
                newApplication.Name, 
                newApplication.Id, 
                request.ApiKey.Substring(0, Math.Min(4, request.ApiKey.Length)));
                
            if (newApplication.IsAdmin)
            {
                _logger.LogWarning("Created application with ADMIN privileges: {AppName} ({AppId})", newApplication.Name, newApplication.Id);
            }
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

        // 6. Return the new ID (consumer will need to remember the plain text key!)
        return newApplication.Id;
    }
}
