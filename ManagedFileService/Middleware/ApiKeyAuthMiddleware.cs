using System.Security.Claims;
using ManagedFileService.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace ManagedFileService.Middleware;


public class ApiKeyAuthMiddleware
{
    private readonly RequestDelegate _next;
    private const string ApiKeyHeaderName = "X-Api-Key";
    private const string ApplicationContextItemKey = "AllowedApplication"; // For CurrentRequestService

    public ApiKeyAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    // No need to inject IAllowedApplicationRepository here if it's scoped and injected into the next handler/controller that needs it after auth.
    // We need it *during* auth check though. Let's keep it injected here for simplicity for now.
    public async Task InvokeAsync(HttpContext context, IAllowedApplicationRepository appRepository)
    {
        // --- Check if the endpoint allows anonymous access ---
        var endpoint = context.GetEndpoint(); // Requires UseRouting() to be called before this middleware
        if (endpoint?.Metadata?.GetMetadata<IAllowAnonymous>() != null)
        {
            await _next(context); // Skip API key check for [AllowAnonymous] endpoints
            return;
        }
        
        //check if the endpoint is HTTP: GET /scalar/{documentName?}
        if (context.Request.Method == HttpMethods.Get && context.Request.Path.StartsWithSegments("/scalar"))
        {
            // Allow anonymous access to this endpoint
            await _next(context);
            return;
        }
        
        //check if the endoint is openapi/v1.json
        if (context.Request.Method == HttpMethods.Get && context.Request.Path.StartsWithSegments("/openapi/v1.json"))
        {
            // Allow anonymous access to this endpoint
            await _next(context);
            return;
        }
        // --- End Anonymous Check ---

        // --- Proceed with API Key Validation (existing logic) ---
        if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var potentialApiKey))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("API Key missing.");
            return;
        }

        var apiKey = potentialApiKey.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("API Key invalid (empty).");
            return;
        }

        var application = await appRepository.FindByApiKeyAsync(apiKey);

        if (application == null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Invalid API Key.");
            return;
        }

        // Store application info for downstream use
        var claims = new List<Claim>
        {
            new Claim("AppId", application.Id.ToString()),
            new Claim(ClaimTypes.Name, application.Name)
        };
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

