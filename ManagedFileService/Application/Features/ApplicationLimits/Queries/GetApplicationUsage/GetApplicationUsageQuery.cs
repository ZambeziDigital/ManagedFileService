using MediatR;

namespace ManagedFileService.Application.Features.ApplicationLimits.Queries.GetApplicationUsage;

public record GetApplicationUsageQuery(Guid ApplicationId) : IRequest<ApplicationUsageDto>;
