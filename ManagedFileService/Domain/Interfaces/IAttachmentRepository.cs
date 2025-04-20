namespace ManagedFileService.Domain.Interfaces;

public interface IAttachmentRepository
{
    Task AddAsync(Attachment attachment, CancellationToken cancellationToken = default);
    Task<Attachment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeleteAsync(Attachment attachment, CancellationToken cancellationToken = default);
}