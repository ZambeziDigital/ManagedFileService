// // using ManagedFileService.Application.Features.Files.Queries;
// using ManagedFileService.Application.Interfaces;
// using Microsoft.AspNetCore.Mvc;
//
// namespace ManagedFileService.API.Controllers;
//
// [ApiController]
// [Route("api/[controller]")]
// public class FileArchiveController : ControllerBase
// {
//     private readonly ISender _mediator;
//     private readonly ICurrentRequestService _currentRequestService;
//
//     public FileArchiveController(ISender mediator, ICurrentRequestService currentRequestService)
//     {
//         _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
//         _currentRequestService = currentRequestService ?? throw new ArgumentNullException(nameof(currentRequestService));
//     }
//
//     /// <summary>
//     /// Downloads all files for the authenticated application as a ZIP archive
//     /// </summary>
//     [HttpGet("download-all", Name = "DownloadAllFiles")]
//     [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
//     [ProducesResponseType(StatusCodes.Status404NotFound)]
//     [ProducesResponseType(StatusCodes.Status401Unauthorized)]
//     public async Task<IActionResult> DownloadAllFiles()
//     {
//         try
//         {
//             var application = _currentRequestService.GetApplication();
//             var query = new DownloadAllFilesAsZipQuery(application.Id);
//             var result = await _mediator.Send(query);
//
//             if (result.ZipStream == null)
//             {
//                 return NotFound("No files found to download.");
//             }
//
//             return File(result.ZipStream, "application/zip", $"{application.Name}_files_{DateTime.UtcNow:yyyyMMdd}.zip");
//         }
//         catch (Exception ex)
//         {
//             return StatusCode(StatusCodes.Status500InternalServerError, 
//                 new { Message = "An error occurred while creating the ZIP archive." });
//         }
//     }
//
//     /// <summary>
//     /// Downloads files for a specific account as a ZIP archive
//     /// </summary>
//     [HttpGet("download-by-account/{accountId:guid}", Name = "DownloadFilesByAccount")]
//     [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
//     [ProducesResponseType(StatusCodes.Status404NotFound)]
//     [ProducesResponseType(StatusCodes.Status403Forbidden)]
//     [ProducesResponseType(StatusCodes.Status401Unauthorized)]
//     public async Task<IActionResult> DownloadFilesByAccount(Guid accountId)
//     {
//         try
//         {
//             var application = _currentRequestService.GetApplication();
//             var query = new DownloadAccountFilesAsZipQuery(application.Id, accountId);
//             var result = await _mediator.Send(query);
//
//             if (result.ZipStream == null)
//             {
//                 return NotFound("No files found to download.");
//             }
//
//             return File(result.ZipStream, "application/zip", $"account_{accountId}_files_{DateTime.UtcNow:yyyyMMdd}.zip");
//         }
//         catch (NotFoundException)
//         {
//             return NotFound();
//         }
//         catch (ForbiddenAccessException)
//         {
//             return Forbid();
//         }
//         catch (Exception)
//         {
//             return StatusCode(StatusCodes.Status500InternalServerError,
//                 new { Message = "An error occurred while creating the ZIP archive." });
//         }
//     }
// }
