using ManagedFileService.Application.Interfaces;
using ManagedFileService.Domain.Interfaces;
using MediatR;
using System.Reflection;

namespace ManagedFileService.Application.Features.System.Queries.GetSystemStatus;

public class GetSystemStatusQueryHandler : IRequestHandler<GetSystemStatusQuery, SystemStatusDto>
{
    private readonly IAllowedApplicationRepository _allowedApplicationRepository;
    private readonly IAttachmentRepository _attachmentRepository;
    private readonly IFileStorageService _fileStorageService;

    public GetSystemStatusQueryHandler(
        IAllowedApplicationRepository allowedApplicationRepository,
        IAttachmentRepository attachmentRepository,
        IFileStorageService fileStorageService)
    {
        _allowedApplicationRepository = allowedApplicationRepository;
        _attachmentRepository = attachmentRepository;
        _fileStorageService = fileStorageService;
    }

    public async Task<SystemStatusDto> Handle(GetSystemStatusQuery request, CancellationToken cancellationToken)
    {
        // // Get application and attachment counts
        // var applicationCount = (await _allowedApplicationRepository.GetAllAsync(cancellationToken)).Count;
        // var attachmentCount = await _attachmentRepository.GetCountAsync(cancellationToken);
        //
        // // Check storage health
        // var storageHealthy = await _fileStorageService.CheckHealthAsync(cancellationToken);
        //
        // // Check database health by successfully retrieving the counts
        // var dbHealthy = true;
        //
        // // Get total storage used
        // var totalStorageBytes = await _attachmentRepository.GetTotalStorageUsedAsync(cancellationToken);
        //
        // // Get version from assembly
        // var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
        //
        // return new SystemStatusDto
        // {
        //     IsHealthy = dbHealthy && storageHealthy,
        //     Version = version,
        //     ServerTime = DateTime.UtcNow,
        //     Components = new Dictionary<string, bool>
        //     {
        //         ["Database"] = dbHealthy,
        //         ["FileStorage"] = storageHealthy
        //     },
        //     TotalStorageUsedBytes = totalStorageBytes,
        //     TotalApplications = applicationCount,
        //     TotalAttachments = attachmentCount
        // };
        throw new NotImplementedException();
    }
}
