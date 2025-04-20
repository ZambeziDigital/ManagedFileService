using ManagedFileService.Data;
using Microsoft.EntityFrameworkCore;

namespace ManagedFileService.Infrastructure.Persistence.Repositories;


public class AllowedApplicationRepository : IAllowedApplicationRepository
{
    private readonly AppDbContext _context;
    private readonly ILogger<AllowedApplicationRepository> _logger;

    public AllowedApplicationRepository(AppDbContext context, ILogger<AllowedApplicationRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AllowedApplication?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var application = await _context.AllowedApplications.FindAsync(new object[] { id }, cancellationToken: cancellationToken);
        if (application == null)
        {
            _logger.LogInformation("AllowedApplication with ID: {AppId} not found.", id);
        }
        return application;
    }

    /// <summary>
    /// Finds an AllowedApplication by verifying the provided plain-text API key against stored hashes.
    /// WARNING: This iterates through applications and performs hashing checks. This can be inefficient
    /// for a large number of applications. Consider alternative authentication flows for high scale.
    /// A better flow might identify the app first (e.g., via header) then verify its key.
    /// </summary>
    /// <param name="apiKey">The plain-text API key provided by the client.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The matching AllowedApplication or null if not found/key invalid.</returns>
    public async Task<AllowedApplication?> FindByApiKeyAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return null; // Cannot match an empty key
        }

        // --- !! PERFORMANCE WARNING !! ---
        // This loads ALL applications (or at least their hashes) into memory for verification.
        // This is OK for a small number of applications but DOES NOT SCALE well.
        // Alternatives:
        // 1. Require an App Identifier (like Name or ID) alongside the API key in the request header.
        //    Then retrieve only that specific app by identifier and verify its key hash.
        // 2. Use a different Authentication scheme designed for scale (JWT, OAuth).
        // 3. Implement key storage optimized for lookup (less common with salted hashes).

        // Let's proceed with the iteration method as per the interface, with the above warning.
        var allApplications = await _context.AllowedApplications
            .AsNoTracking() // Read-only operation, no need to track changes
            .ToListAsync(cancellationToken); // Load necessary data

        _logger.LogDebug("Attempting to validate API key against {AppCount} registered applications.", allApplications.Count);

        foreach (var app in allApplications)
        {
            if (string.IsNullOrEmpty(app.ApiKeyHash))
            {
                // Skip apps with no hash configured (log potentially?)
                 _logger.LogWarning("Application {AppName} ({AppId}) has no API key hash configured.", app.Name, app.Id);
                continue;
            }

            try
            {
                // Securely verify the provided plain-text key against the stored BCrypt hash
                bool isValid = BCrypt.Net.BCrypt.Verify(apiKey, app.ApiKeyHash);

                if (isValid)
                {
                    _logger.LogInformation("API key validated successfully for Application: {AppName} ({AppId})", app.Name, app.Id);
                    // Re-fetch the entity *with tracking* if needed downstream,
                    // or return the non-tracked one if only reading data.
                    // Let's return the non-tracked one found during iteration for efficiency.
                    // If the handler needed to *update* this application entity later
                    // in the same request, it would need re-attaching or fetching with tracking.
                    return app;
                }
            }
            catch (BCrypt.Net.SaltParseException ex)
            {
                // This usually means the stored hash is not a valid BCrypt hash
                _logger.LogError(ex, "Invalid BCrypt hash format encountered for Application {AppName} ({AppId}). Stored Hash: {StoredHash}",
                    app.Name, app.Id, app.ApiKeyHash);
                // Decide how to handle - skip, fail request? Let's skip for now.
                continue;
            }
            catch (Exception ex) // Catch broader exceptions during verification
            {
                 _logger.LogError(ex, "Unexpected error during API key verification for Application {AppName} ({AppId}).", app.Name, app.Id);
                // Decide whether to continue checking others or rethrow
                continue;
            }
        }

        // If no application's key matched after checking all
        _logger.LogWarning("Provided API key did not match any registered application.");
        return null;
    }

    // You might also want methods to ADD or UPDATE applications and their keys.
    // Remember to HASH keys before saving!
    // public async Task AddApplicationAsync(AllowedApplication application, string plainTextApiKey, CancellationToken cancellationToken = default)
    // {
    //     // Hash the key before saving!
    //     application.SetApiKeyHash(BCryptNet.HashPassword(plainTextApiKey)); // Add SetApiKeyHash method to entity or do it here
    //     await _context.AllowedApplications.AddAsync(application, cancellationToken);
    //     await _context.SaveChangesAsync(cancellationToken);
    // }
    
    
    public async Task AddAsync(AllowedApplication application, CancellationToken cancellationToken = default)
    {
        if (application == null) throw new ArgumentNullException(nameof(application));
        if (string.IsNullOrWhiteSpace(application.ApiKeyHash))
        {
            // Safety check - ensure hash was generated before trying to save
            _logger.LogError("Attempted to add Application {AppName} ({AppId}) with an empty ApiKeyHash.", application.Name, application.Id);
            throw new InvalidOperationException("Cannot save an application without a valid API Key hash.");
        }

        await _context.AllowedApplications.AddAsync(application, cancellationToken);
        // Consider Unit of Work pattern - saving might happen higher up
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Added AllowedApplication to DB: {AppName} ({AppId})", application.Name, application.Id);
    }
}