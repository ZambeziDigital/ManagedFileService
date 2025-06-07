using ManagedFileService.Domain.Entities;
using ManagedFileService.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ManagedFileService.Data;

public static class SeedData
{
    // This constant is exposed so you can use it in tests or debugging
    public const string TestApiKey = "test_api_key_secure_enough_16chars";
    
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();
        var appRepo = scope.ServiceProvider.GetRequiredService<IAllowedApplicationRepository>();
        
        logger.LogInformation("Checking for seed data requirements");
        
        // Check if we have any applications at all
        var hasApplications = await dbContext.AllowedApplications.AnyAsync();
        
        if (!hasApplications)
        {
            logger.LogInformation("No applications found in database, creating test application");
            
            // Create a test application with a known API key for development
            string apiKeyHash = BCrypt.Net.BCrypt.HashPassword(TestApiKey);
            
            var testApp = new AllowedApplication(
                name: "Test Application", 
                apiKeyHash: apiKeyHash,
                maxFileSizeBytes: 10 * 1024 * 1024, // 10 MB
                isAdmin: true);
                
            await appRepo.AddAsync(testApp);
            
            logger.LogInformation("Created test application with ID: {AppId}", testApp.Id);
            logger.LogWarning("TEST API KEY FOR DEVELOPMENT: {ApiKey}", TestApiKey);
        }
    }
}
