using ManagedFileService.Application.Features.System.Queries;
using ManagedFileService.Application.Features.System.Queries.GetSystemStatus;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ManagedFileService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SystemStatusController : ControllerBase
{
    private readonly ISender _mediator;
    private readonly ILogger<SystemStatusController> _logger;

    public SystemStatusController(ISender mediator, ILogger<SystemStatusController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Check if the service is up and running
    /// </summary>
    [HttpGet("health", Name = "HealthCheck")]
    [AllowAnonymous] // Allow anonymous access for health checks
    [ProducesResponseType(typeof(HealthCheckResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> HealthCheck()
    {
        // try
        // {
        //     var query = new HealthCheckQuery();
        //     var result = await _mediator.Send(query);
        //     
        //     if (result.Status == "Healthy")
        //     {
        //         return Ok(result);
        //     }
        //     else
        //     {
        //         return StatusCode(StatusCodes.Status503ServiceUnavailable, result);
        //     }
        // }
        // catch (Exception ex)
        // {
        //     _logger.LogError(ex, "Error during health check");
        //     return StatusCode(StatusCodes.Status503ServiceUnavailable, 
        //         new HealthCheckResponse("Unhealthy", "Error during health check", DateTime.UtcNow));
        // }
        var response = new HealthCheckResponse("Healthy", "Service is running", DateTime.UtcNow);
        return Ok(response);
    }

    /// <summary>
    /// Get detailed system status - requires authentication
    /// </summary>
    [HttpGet("details", Name = "SystemDetails")]
    [ProducesResponseType(typeof(SystemStatusDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SystemStatusDetailsDto>> GetSystemDetails()
    {
        try
        {
            var query = new GetSystemStatusQuery();
            var result = await _mediator.Send(query);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving system details");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { Message = "An error occurred while retrieving system status." });
        }
    }
}

public record HealthCheckResponse(string Status, string Message, DateTime CheckedAt);

public record SystemStatusDetailsDto(
    string ServiceName,
    string Version,
    string Environment,
    long TotalStorageUsedBytes,
    int TotalAttachments,
    int TotalApplications,
    bool DatabaseConnected,
    bool FileStorageConnected,
    DateTime UptimeSince,
    string[] ActiveFeatures);
