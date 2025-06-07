global using Microsoft.AspNetCore.Mvc;
global using ManagedFileService.API.Models;
global using System.Security.Claims;
global using ManagedFileService.Application.Features.Attachments.Commands.CreateAllowedApplication;
global using ManagedFileService.Application.Features.Attachments.Queries.GetAttachmentMetadata;
using Microsoft.AspNetCore.Authorization;

namespace ManagedFileService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly IAllowedApplicationRepository _applicationRepository;
    private readonly IAttachmentRepository _attachmentRepository;
    private readonly ISender _mediator;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IAllowedApplicationRepository applicationRepository,
        IAttachmentRepository attachmentRepository,
        ISender mediator,
        ILogger<AdminController> logger)
    {
        _applicationRepository = applicationRepository;
        _attachmentRepository = attachmentRepository;
        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(DashboardStatsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetDashboardStats(CancellationToken cancellationToken)
    {
        // Check if caller is admin
        if (!IsAdmin())
        {
            return Forbid("Admin access required");
        }

        // Get statistics for dashboard
        var totalAttachments = await _attachmentRepository.GetTotalCountAsync(cancellationToken);
        var totalStorageBytes = await _attachmentRepository.GetTotalStorageBytesAsync(cancellationToken);
        var applications = await _applicationRepository.GetAllAsync(cancellationToken);
        var storageByApp = await _attachmentRepository.GetStorageByApplicationAsync(cancellationToken);

        // Map application stats
        var applicationStats = applications.Select(app => new ApplicationStatsDto
        {
            Id = app.Id,
            Name = app.Name,
            IsAdmin = app.IsAdmin,
            MaxFileSizeBytes = app.MaxFileSizeBytes,
            TotalStorageBytes = storageByApp.TryGetValue(app.Id, out var size) ? size : 0
        }).ToList();

        // Create dashboard DTO
        var dashboard = new DashboardStatsDto
        {
            TotalAttachments = totalAttachments,
            TotalStorageBytes = totalStorageBytes,
            TotalApplications = applications.Count,
            ApplicationStats = applicationStats
        };

        return Ok(dashboard);
    }

    [HttpGet("applications")]
    [ProducesResponseType(typeof(List<ApplicationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllApplications(CancellationToken cancellationToken)
    {
        // Check if caller is admin
        if (!IsAdmin())
        {
            return Forbid("Admin access required");
        }

        var applications = await _applicationRepository.GetAllAsync(cancellationToken);
        
        var applicationDtos = applications.Select(app => new ApplicationDto
        {
            Id = app.Id,
            Name = app.Name,
            IsAdmin = app.IsAdmin,
            MaxFileSizeBytes = app.MaxFileSizeBytes
        }).ToList();

        return Ok(applicationDtos);
    }
    
    [HttpGet("applications/{id}")]
    [ProducesResponseType(typeof(ApplicationDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetApplicationById(Guid id, CancellationToken cancellationToken)
    {
        // Check if caller is admin
        if (!IsAdmin())
        {
            return Forbid("Admin access required");
        }
        
        var application = await _applicationRepository.GetByIdAsync(id, cancellationToken);
        if (application == null)
        {
            return NotFound();
        }

        // Get storage usage for this application
        var storageByApp = await _attachmentRepository.GetStorageByApplicationAsync(cancellationToken);
        var storageUsed = storageByApp.TryGetValue(id, out var size) ? size : 0;
        
        var attachments = await _attachmentRepository.GetByApplicationIdAsync(id, 1, 20, cancellationToken);
        
        var applicationDto = new ApplicationDetailDto
        {
            Id = application.Id,
            Name = application.Name,
            IsAdmin = application.IsAdmin,
            MaxFileSizeBytes = application.MaxFileSizeBytes,
            TotalStorageBytes = storageUsed,
            RecentAttachments = attachments.Select(att => new AttachmentMetadataDto
            (
                att.Id,
                att.OriginalFileName,
                att.ContentType,
                att.SizeBytes,
                att.UploadedAtUtc,
                att.UserId
            )).ToList()
        };

        return Ok(applicationDto);
    }
    
    [HttpPost("applications")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateApplication([FromBody] CreateApplicationRequest request, CancellationToken cancellationToken)
    {
        // Check if caller is admin
        if (!IsAdmin())
        {
            return Forbid("Admin access required");
        }
        
        try
        {
            var command = new CreateAllowedApplicationCommand(
                request.Name,
                request.ApiKey,
                request.MaxFileSizeMegaBytes,
                request.IsAdmin);
                
            var newAppId = await _mediator.Send(command, cancellationToken);
            
            return CreatedAtAction(nameof(GetApplicationById), new { id = newAppId }, newAppId);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating application");
            return StatusCode(500, new { Message = "An error occurred while creating the application" });
        }
    }
    
    [HttpDelete("applications/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteApplication(Guid id, CancellationToken cancellationToken)
    {
        // Check if caller is admin
        if (!IsAdmin())
        {
            return Forbid("Admin access required");
        }
        
        var application = await _applicationRepository.GetByIdAsync(id, cancellationToken);
        if (application == null)
        {
            return NotFound();
        }
        
        // Get all attachments for this application to delete them too
        var attachments = await _attachmentRepository.GetByApplicationIdAsync(id, 1, int.MaxValue, cancellationToken);
        
        // Delete all attachments first
        foreach (var attachment in attachments)
        {
            await _attachmentRepository.DeleteAsync(attachment.Id, cancellationToken);
        }
        
        // Delete the application
        await _applicationRepository.DeleteAsync(id, cancellationToken);
        
        return NoContent();
    }
    
    [HttpGet("attachments")]
    [ProducesResponseType(typeof(PaginatedList<AttachmentMetadataDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllAttachments(
        [FromQuery] int pageNumber = 1, 
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        // Check if caller is admin
        if (!IsAdmin())
        {
            return Forbid("Admin access required");
        }
        
        // Validate pagination parameters
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;
        
        var totalCount = await _attachmentRepository.GetTotalCountAsync(cancellationToken);
        var attachments = await _attachmentRepository.GetAllAsync(pageNumber, pageSize, cancellationToken);
        
        // Get all applications to map names
        var applications = await _applicationRepository.GetAllAsync(cancellationToken);
        var appDict = applications.ToDictionary(a => a.Id, a => a.Name);
        
        var attachmentDtos = attachments.Select(att => new AttachmentListItemDto
        {
            Id = att.Id,
            OriginalFileName = att.OriginalFileName,
            ContentType = att.ContentType,
            SizeBytes = att.SizeBytes,
            UploadedAtUtc = att.UploadedAtUtc,
            UserId = att.UserId,
            ApplicationId = att.ApplicationId,
            ApplicationName = appDict.TryGetValue(att.ApplicationId, out var name) ? name : "Unknown"
        }).ToList();
        
        var result = new PaginatedList<AttachmentListItemDto>
        {
            Items = attachmentDtos,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
        
        return Ok(result);
    }
    
    [HttpGet("storage/summary")]
    [ProducesResponseType(typeof(StorageSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetStorageSummary(CancellationToken cancellationToken)
    {
        // Check if caller is admin
        if (!IsAdmin())
        {
            return Forbid("Admin access required");
        }
        
        var totalStorageBytes = await _attachmentRepository.GetTotalStorageBytesAsync(cancellationToken);
        var storageByApp = await _attachmentRepository.GetStorageByApplicationAsync(cancellationToken);
        var applications = await _applicationRepository.GetAllAsync(cancellationToken);
        
        var appStorage = applications.Select(app => new ApplicationStorageDto
        {
            ApplicationId = app.Id,
            ApplicationName = app.Name,
            StorageBytes = storageByApp.TryGetValue(app.Id, out var size) ? size : 0,
            StorageMegabytes = storageByApp.TryGetValue(app.Id, out size) ? Math.Round(size / 1048576.0, 2) : 0
        }).OrderByDescending(a => a.StorageBytes).ToList();
        
        var summary = new StorageSummaryDto
        {
            TotalStorageBytes = totalStorageBytes,
            TotalStorageMegabytes = Math.Round(totalStorageBytes / 1048576.0, 2),
            ApplicationStorage = appStorage
        };
        
        return Ok(summary);
    }
    
    // Helper method to check if the current user is an admin
    private bool IsAdmin()
    {
        return User.HasClaim(ClaimTypes.Role, "Admin");
    }
}
