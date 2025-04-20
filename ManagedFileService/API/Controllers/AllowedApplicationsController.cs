using ManagedFileService.Application.Features.Attachments.Commands.CreateAllowedApplication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ManagedFileService.API.Controllers;


[ApiController]
[Route("api/[controller]")]
public class AllowedApplicationsController(ISender mediator) : ControllerBase
{
    private readonly ISender _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

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
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
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
                request.MaxFileSizeMegaBytes
            );

            var newAppId = await _mediator.Send(command);

            // Return 201 Created with the new ID and optionally a location header
            // Assume a "GetApplicationById" route name exists for the Location header
            // return CreatedAtAction("GetApplicationById", new { id = newAppId }, newAppId);
            // If you don't have/want a GetById endpoint yet, you can return Ok:
            return Ok(newAppId);
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

    // Placeholder for the CreatedAtAction reference. Implement later if needed.
    // [HttpGet("{id:guid}", Name = "GetApplicationById")]
    // [ProducesResponseType(StatusCodes.Status404NotFound)]
    // public IActionResult GetApplicationById(Guid id)
    // {
    //     // *** TODO: Implement Get Application endpoint logic later ***
    //     // *** TODO: Add Authentication/Authorization check here later! ***
    //     return NotFound(); // Placeholder implementation
    // }
}