using MediatR;

namespace ManagedFileService.Application.Features.Files.Commands.CreateArchive;

public record CreateArchiveCommand(IEnumerable<Guid> AttachmentIds, string ArchiveName) : IRequest<CreateArchiveResponse>;
