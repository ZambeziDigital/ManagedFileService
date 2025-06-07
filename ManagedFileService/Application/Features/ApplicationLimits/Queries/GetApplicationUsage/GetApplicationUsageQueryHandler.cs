using ManagedFileService.Application.Common.Exceptions;
using ManagedFileService.Application.Interfaces;
using ManagedFileService.Domain.Interfaces;
using MediatR;

namespace ManagedFileService.Application.Features.ApplicationLimits.Queries.GetApplicationUsage;

public class GetApplicationUsageQueryHandler : IRequestHandler<GetApplicationUsageQuery, ApplicationUsageDto>
{
    private readonly IAllowedApplicationRepository _applicationRepository;
    private readonly IAttachmentRepository _attachmentRepository;
    private readonly ICurrentRequestService _currentRequestService;

    public GetApplicationUsageQueryHandler(
        IAllowedApplicationRepository applicationRepository,
        IAttachmentRepository attachmentRepository,
        ICurrentRequestService currentRequestService)
    {
        _applicationRepository = applicationRepository;
        _attachmentRepository = attachmentRepository;
        _currentRequestService = currentRequestService;
    }

    public async Task<ApplicationUsageDto> Handle(GetApplicationUsageQuery request, CancellationToken cancellationToken)
    {
        // Authorization check - admin can check any app, others can only check themselves
        var currentAppId = _currentRequestService.GetApplicationId();
        var currentApp = await _applicationRepository.GetByIdAsync(currentAppId, cancellationToken);
        
        if (currentApp == null)
        {
            throw new UnauthorizedAccessException("Application not found");
        }
        
        // Non-admin apps can only view their own statistics
        if (!currentApp.IsAdmin && currentAppId != request.ApplicationId)
        {
            throw new ForbiddenAccessException("You can only view statistics for your own application");
        }

        // Get the target application
        var targetApp = await _applicationRepository.GetByIdAsync(request.ApplicationId, cancellationToken);
        if (targetApp == null)
        {
            throw new NotFoundException(nameof(AllowedApplication), request.ApplicationId);
        }

        // Get storage usage
        var storageUsage = await _attachmentRepository.GetStorageBytesForApplicationAsync(request.ApplicationId, cancellationToken);
        
        // Get total files count
        int totalFiles = await _attachmentRepository.GetTotalCountForApplicationAsync(request.ApplicationId, cancellationToken);

        return new ApplicationUsageDto
        {
            ApplicationId = targetApp.Id,
            ApplicationName = targetApp.Name,
            CurrentStorageBytes = storageUsage,
            MaxFileSizeBytes = targetApp.MaxFileSizeBytes,
            MaxStorageBytes = targetApp.MaxStorageBytes,
            TotalFiles = totalFiles
        };
    }
}
