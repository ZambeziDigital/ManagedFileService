using ManagedFileService.Application.Common.Exceptions;
using ManagedFileService.Application.Interfaces;
using ManagedFileService.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ManagedFileService.Application.Features.ApplicationLimits.Commands.UpdateApplicationLimits;

public class UpdateApplicationLimitsCommandHandler : IRequestHandler<UpdateApplicationLimitsCommand>
{
    private readonly IAllowedApplicationRepository _applicationRepository;
    private readonly ICurrentRequestService _currentRequestService;
    private readonly ILogger<UpdateApplicationLimitsCommandHandler> _logger;

    public UpdateApplicationLimitsCommandHandler(
        IAllowedApplicationRepository applicationRepository,
        ICurrentRequestService currentRequestService,
        ILogger<UpdateApplicationLimitsCommandHandler> logger)
    {
        _applicationRepository = applicationRepository;
        _currentRequestService = currentRequestService;
        _logger = logger;
    }

    public async Task Handle(UpdateApplicationLimitsCommand request, CancellationToken cancellationToken)
    {
        // Authorization check - only admin applications can update limits
        var currentAppId = _currentRequestService.GetApplicationId();
        var currentApp = await _applicationRepository.GetByIdAsync(currentAppId, cancellationToken);
        
        if (currentApp == null || !currentApp.IsAdmin)
        {
            _logger.LogWarning("Non-admin application {AppId} attempted to update limits for {TargetAppId}", 
                currentAppId, request.ApplicationId);
            throw new ForbiddenAccessException("Only admin applications can update application limits");
        }

        // Get target application
        var targetApp = await _applicationRepository.GetByIdAsync(request.ApplicationId, cancellationToken);
        if (targetApp == null)
        {
            throw new NotFoundException(nameof(AllowedApplication), request.ApplicationId);
        }

        // Convert MB to bytes if provided (1 MB = 1,048,576 bytes)
        if (request.MaxFileSizeMegaBytes.HasValue)
        {
            targetApp.SetMaxFileSize(request.MaxFileSizeMegaBytes.Value * 1_048_576);
        }

        if (request.MaxStorageMegaBytes.HasValue)
        {
            targetApp.SetMaxStorageSize(request.MaxStorageMegaBytes.Value * 1_048_576);
        }

        await _applicationRepository.UpdateAsync(targetApp, cancellationToken);
        
        _logger.LogInformation(
            "Updated limits for application {AppName} ({AppId}): MaxFileSize={MaxFileSize}MB, MaxStorage={MaxStorage}MB", 
            targetApp.Name, 
            targetApp.Id, 
            request.MaxFileSizeMegaBytes, 
            request.MaxStorageMegaBytes);
    }
}
