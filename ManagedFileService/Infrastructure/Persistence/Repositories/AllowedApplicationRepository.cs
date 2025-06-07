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
            _logger.LogWarning("Empty API key provided to FindByApiKeyAsync");
            return null; // Cannot match an empty key
        }

        // Load applications
        var allApplications = await _context.AllowedApplications
            .AsNoTracking() // Read-only operation, no need to track changes
            .ToListAsync(cancellationToken); // Load necessary data

        _logger.LogDebug("Attempting to validate API key against {AppCount} registered applications.", allApplications.Count);
        
        if (allApplications.Count == 0)
        {
            _logger.LogWarning("No applications found in database during API key validation");
        }

        foreach (var app in allApplications)
        {
            if (string.IsNullOrEmpty(app.ApiKeyHash))
            {
                _logger.LogWarning("Application {AppName} ({AppId}) has no API key hash configured.", app.Name, app.Id);
                continue;
            }

            _logger.LogDebug("Checking key against application: {AppName} ({AppId})", app.Name, app.Id);
            
            try
            {
                // Log hash format information (without revealing the actual hash)
                _logger.LogDebug("Hash format check for {AppName}: Length={HashLength}, StartsWithBCrypt={StartsWithBCrypt}",
                    app.Name, 
                    app.ApiKeyHash.Length,
                    app.ApiKeyHash.StartsWith("$2a$") || app.ApiKeyHash.StartsWith("$2b$") || app.ApiKeyHash.StartsWith("$2y$"));
                
                // Securely verify the provided plain-text key against the stored BCrypt hash
                bool isValid = BCrypt.Net.BCrypt.Verify(apiKey, app.ApiKeyHash);

                _logger.LogDebug("Verification result for {AppName}: {Result}", app.Name, isValid ? "Match" : "No match");
                
                if (isValid)
                {
                    _logger.LogInformation("API key validated successfully for Application: {AppName} ({AppId})", app.Name, app.Id);
                    return app;
                }
            }
            catch (BCrypt.Net.SaltParseException ex)
            {
                // This usually means the stored hash is not a valid BCrypt hash
                _logger.LogError(ex, "Invalid BCrypt hash format encountered for Application {AppName} ({AppId}). Hash starts with: {HashStart}",
                    app.Name, app.Id, app.ApiKeyHash.Substring(0, Math.Min(10, app.ApiKeyHash.Length)));
                continue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during API key verification for Application {AppName} ({AppId}).", app.Name, app.Id);
                continue;
            }
        }

        // If no application's key matched after checking all
        _logger.LogWarning("Provided API key did not match any registered applications.");
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
    
    public async Task<IReadOnlyList<AllowedApplication>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.AllowedApplications
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
    
    public async Task UpdateAsync(AllowedApplication application, CancellationToken cancellationToken = default)
    {
        if (application == null) throw new ArgumentNullException(nameof(application));
        
        _context.Entry(application).State = EntityState.Modified;
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Updated AllowedApplication: {AppName} ({AppId})", application.Name, application.Id);
    }
    
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var application = await _context.AllowedApplications.FindAsync(new object[] { id }, cancellationToken);
        if (application == null)
        {
            _logger.LogWarning("Attempted to delete non-existent AllowedApplication with ID: {AppId}", id);
            return;
        }
        
        _context.AllowedApplications.Remove(application);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Deleted AllowedApplication: {AppName} ({AppId})", application.Name, application.Id);
    }
    
    public async Task<IReadOnlyList<AllowedApplication>> GetAdminApplicationsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.AllowedApplications
            .AsNoTracking()
            .Where(a => a.IsAdmin)
            .ToListAsync(cancellationToken);
    }
}