using Microsoft.AspNetCore.Mvc;

namespace ManagedFileService.API.Controllers;

[ApiController]
[Route("api/test")]
public class TestAuthController : ControllerBase
{
    private readonly ILogger<TestAuthController> _logger;
    
    public TestAuthController(ILogger<TestAuthController> logger)
    {
        _logger = logger;
    }
    
    [HttpGet("auth")]
    [ProducesResponseType(typeof(AuthStatusResponse), StatusCodes.Status200OK)]
    public IActionResult TestAuth()
    {
        // This endpoint requires authentication through the ApiKeyAuthMiddleware
        // If we get here, authentication succeeded
        
        var response = new AuthStatusResponse(
            Status: "Authenticated",
            Message: "Your API key is valid",
            AppName: User.Identity?.Name ?? "Unknown",
            AppId: User.FindFirst("AppId")?.Value ?? "Unknown",
            IsAdmin: User.IsInRole("Admin")
        );
        
        _logger.LogInformation("Authentication test successful for {AppName}", response.AppName);
        
        return Ok(response);
    }
    
    public record AuthStatusResponse(
        string Status,
        string Message,
        string AppName,
        string AppId,
        bool IsAdmin
    );
}
