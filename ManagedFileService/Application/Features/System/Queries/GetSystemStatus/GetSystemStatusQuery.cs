using MediatR;

namespace ManagedFileService.Application.Features.System.Queries.GetSystemStatus;

public record GetSystemStatusQuery : IRequest<SystemStatusDto>;
