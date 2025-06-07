using ManagedFileService.Application.Features.ApplicationLimits.Commands.UpdateApplicationLimits;
using ManagedFileService.Application.Features.ApplicationLimits.Queries.GetApplicationUsage;
using ManagedFileService.Application.Features.Attachments.Commands.CreateAllowedApplication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ManagedFileService.API.Controllers;


[ApiController]
[Route("api/[controller]")]
public class AllowedApplicationsController : ControllerBase
{
    private readonly ISender _mediator;

    public AllowedApplicationsController(ISender mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    /// <summary>
    /// Creates a new allowed application that can use the Attachment Service.
    /// </summary>
    /// <remarks>
    /// **WARNING:** This endpoint is currently UNPROTECTED. In a real application,
    /// this endpoint MUST be secured to prevent unauthorized creation of applications.
    /// The provided 'ApiKey' will be securely hashed before storage.
    /// </remarks>
    /// <param name="request">Details of the application to create.</param>
    /// <returns>The ID of the newly created application.</returns>
    [HttpPost(Name = "CreateAllowedApplication")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CreateApplicationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)] // For validation errors
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateApplication([FromBody] CreateAllowedApplicationRequest request)
    {
        // *** TODO: Add Authentication/Authorization check here later! ***
        // For now, anyone can call this.

        if (!ModelState.IsValid) // Basic model validation
        {
            return BadRequest(ModelState);
        }

        try
        {
            var command = new CreateAllowedApplicationCommand(
                request.Name,
                request.ApiKey, // Pass the plain text key to the command
                request.MaxFileSizeMegaBytes,
                false // This endpoint cannot create admin applications
            );

            var newAppId = await _mediator.Send(command);

            // Return a response with explicit instructions
            var response = new CreateApplicationResponse(
                Id: newAppId,
                Name: request.Name,
                ApiKeyInstructions: $"IMPORTANT: Use the original API key '{request.ApiKey.Substring(0, Math.Min(4, request.ApiKey.Length))}...' that you provided (not this Application ID) for authentication."
            );

            return Ok(response);
        }
        catch (ValidationException ex)
        {
            // Handle validation errors specifically defined in the handler
            return BadRequest(new { ex.Message }); // Return a simple error object
        }
        catch (InfrastructureException)
        {
             // Handle errors related to hashing or DB interaction during creation
            // Logged in handler, return a generic server error
             return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "An internal error occurred while creating the application." });
        }
        // Let the global error handler catch other unexpected exceptions
    }

    /// <summary>
    /// Updates an application's file size and storage limits.
    /// Requires admin access.
    /// </summary>
    [HttpPut("{id:guid}/limits", Name = "UpdateApplicationLimits")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateLimits(Guid id, [FromBody] UpdateApplicationLimitsRequest request)
    {
        try
        {
            var command = new UpdateApplicationLimitsCommand(
                id,
                request.MaxFileSizeMegaBytes,
                request.MaxStorageMegaBytes);

            await _mediator.Send(command);
            return Ok();
        }
        catch (NotFoundException)
        {
            return NotFound($"Application with ID {id} not found.");
        }
        catch (ForbiddenAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { Message = "An error occurred while updating application limits." });
        }
    }

    /// <summary>
    /// Gets storage usage statistics for an application.
    /// </summary>
    [HttpGet("{id:guid}/usage", Name = "GetApplicationUsage")]
    [ProducesResponseType(typeof(ApplicationUsageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApplicationUsageDto>> GetUsage(Guid id)
    {
        try
        {
            var query = new GetApplicationUsageQuery(id);
            var result = await _mediator.Send(query);
            return Ok(result);
        }
        catch (NotFoundException)
        {
            return NotFound($"Application with ID {id} not found.");
        }
        catch (ForbiddenAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { Message = "An error occurred while retrieving usage statistics." });
        }
    }
}

public record CreateApplicationResponse(
    Guid Id,
    string Name,
    string ApiKeyInstructions
);

public class UpdateApplicationLimitsRequest
{
    public long? MaxFileSizeMegaBytes { get; set; }
    public long? MaxStorageMegaBytes { get; set; }
}