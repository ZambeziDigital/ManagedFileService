using ManagedFileService.Application.Common.Exceptions;
using ManagedFileService.Application.Interfaces;
using ManagedFileService.Domain.Interfaces;
using MediatR;

namespace ManagedFileService.Application.Features.Files.Commands.CreateArchive;

public class CreateArchiveCommandHandler : IRequestHandler<CreateArchiveCommand, CreateArchiveResponse>
{
    private readonly IAttachmentRepository _attachmentRepository;
    private readonly IZipArchiveService _zipArchiveService;
    private readonly ISignedUrlService _signedUrlService;
    private readonly ICurrentRequestService _currentRequestService;

    public CreateArchiveCommandHandler(
        IAttachmentRepository attachmentRepository,
        IZipArchiveService zipArchiveService,
        ISignedUrlService signedUrlService,
        ICurrentRequestService currentRequestService)
    {
        _attachmentRepository = attachmentRepository;
        _zipArchiveService = zipArchiveService;
        _signedUrlService = signedUrlService;
        _currentRequestService = currentRequestService;
    }

    public async Task<CreateArchiveResponse> Handle(CreateArchiveCommand request, CancellationToken cancellationToken)
    {
        // var applicationId = _currentRequestService.GetApplicationId();
        //
        // // Get all the requested attachments that belong to the current application
        // var attachments = await _attachmentRepository.GetAttachmentsByIdsAsync(request.AttachmentIds, cancellationToken);
        //
        // // Verify the attachments belong to the current application
        // attachments = attachments.Where(a => a.ApplicationId == applicationId).ToList();
        //
        // if (!attachments.Any())
        // {
        //     throw new NotFoundException("Attachments", string.Join(", ", request.AttachmentIds));
        // }
        //
        // // Create a proper archive name if not provided
        // var archiveName = string.IsNullOrWhiteSpace(request.ArchiveName)
        //     ? $"archive_{DateTime.UtcNow:yyyyMMdd_HHmmss}"
        //     : request.ArchiveName;
        //
        // // Create the archive
        // var archiveResult = await _zipArchiveService.CreateArchiveFromAttachmentsAsync(
        //     attachments, 
        //     archiveName,
        //     cancellationToken);
        //     
        // // Generate a URL for downloading the archive
        // var baseUrl = "api/files/download"; // Update with your actual download URL
        // var signedUrl = _signedUrlService.GenerateSignedUrl(
        //     archiveResult.ArchiveId,
        //     1440, // 24 hours
        //     baseUrl);
        //     
        // return new CreateArchiveResponse
        // {
        //     ArchiveId = archiveResult.ArchiveId,
        //     DownloadUrl = signedUrl.Url,
        //     FileCount = attachments.Count(),
        //     TotalSizeBytes = attachments.Sum(a => a.SizeInBytes)
        // };
        throw new NotImplementedException();
    }
}
