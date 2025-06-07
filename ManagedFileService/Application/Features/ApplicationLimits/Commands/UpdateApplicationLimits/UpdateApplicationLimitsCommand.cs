using MediatR;

namespace ManagedFileService.Application.Features.ApplicationLimits.Commands.UpdateApplicationLimits;

public record UpdateApplicationLimitsCommand(
    Guid ApplicationId,
    long? MaxFileSizeMegaBytes,
    long? MaxStorageMegaBytes) : IRequest;
