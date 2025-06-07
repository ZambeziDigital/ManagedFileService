using System.Security.Claims;
using ManagedFileService.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace ManagedFileService.Middleware;

public class ApiKeyAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiKeyAuthMiddleware> _logger;
    private const string ApiKeyHeaderName = "X-Api-Key";
    private const string ApplicationContextItemKey = "AllowedApplication"; // For CurrentRequestService

    public ApiKeyAuthMiddleware(RequestDelegate next, ILogger<ApiKeyAuthMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    // No need to inject IAllowedApplicationRepository here if it's scoped and injected into the next handler/controller that needs it after auth.
    // We need it *during* auth check though. Let's keep it injected here for simplicity for now.
    public async Task InvokeAsync(HttpContext context, IAllowedApplicationRepository appRepository)
    {
        // --- Check if the endpoint allows anonymous access ---
        var endpoint = context.GetEndpoint(); // Requires UseRouting() to be called before this middleware
        if (endpoint?.Metadata?.GetMetadata<IAllowAnonymous>() != null)
        {
            _logger.LogDebug("Endpoint has [AllowAnonymous] attribute, skipping API key check");
            await _next(context); // Skip API key check for [AllowAnonymous] endpoints
            return;
        }
        
        //check if the endpoint is HTTP: GET /scalar/{documentName?}
        if (context.Request.Method == HttpMethods.Get && context.Request.Path.StartsWithSegments("/scalar"))
        {
            _logger.LogDebug("Skipping API key check for Scalar documentation endpoint");
            // Allow anonymous access to this endpoint
            await _next(context);
            return;
        }
        
        //check if the endpoint is openapi/v1.json
        if (context.Request.Method == HttpMethods.Get && context.Request.Path.StartsWithSegments("/openapi/v1.json"))
        {
            _logger.LogDebug("Skipping API key check for OpenAPI endpoint");
            // Allow anonymous access to this endpoint
            await _next(context);
            return;
        }
        
        //check if the endpoint is health check
        if (context.Request.Method == HttpMethods.Get && context.Request.Path.StartsWithSegments("/api/systemstatus/health"))
        {
            _logger.LogDebug("Skipping API key check for health check endpoint");
            // Allow anonymous access to health check
            await _next(context);
            return;
        }
        // --- End Anonymous Check ---

        // --- Proceed with API Key Validation (existing logic) ---
        _logger.LogDebug("Checking API key for path: {Path}", context.Request.Path);
        
        if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var potentialApiKey))
        {
            _logger.LogWarning("API Key header '{Header}' missing from request", ApiKeyHeaderName);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("API Key missing.");
            return;
        }

        var apiKey = potentialApiKey.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("API Key header present but value is empty");
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("API Key invalid (empty).");
            return;
        }

        _logger.LogDebug("Received API key: {KeyStart}... (length: {KeyLength})", 
            apiKey.Substring(0, Math.Min(4, apiKey.Length)), apiKey.Length);
        
        var application = await appRepository.FindByApiKeyAsync(apiKey);

        if (application == null)
        {
            _logger.LogWarning("API key validation failed - no matching application found");
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Invalid API Key.");
            return;
        }

        _logger.LogInformation("API key authenticated successfully for application: {AppName} ({AppId})", 
            application.Name, application.Id);
        
        // Store application info for downstream use
        var claims = new List<Claim>
        {
            new Claim("AppId", application.Id.ToString()),
            new Claim(ClaimTypes.Name, application.Name)
        };
        
        // Add admin claim if applicable
        if (application.IsAdmin)
        {
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));
        }
        
        var identity = new ClaimsIdentity(claims, "ApiKey"); // Scheme name
        context.User = new ClaimsPrincipal(identity);

        // Store the full application object for easy access via CurrentRequestService
        context.Items[ApplicationContextItemKey] = application;

        await _next(context);
    }
    
    // Example Hashing (Replace with BCrypt or similar)
    // private string HashApiKey(string apiKey)
    // {
    //     // Use a secure hashing algorithm like Argon2 or BCrypt
    //     // This is a placeholder - DO NOT USE IN PRODUCTION
    //     using var sha256 = System.Security.Cryptography.SHA256.Create();
    //     var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(apiKey));
    //     return Convert.ToBase64String(bytes);
    // }
}

