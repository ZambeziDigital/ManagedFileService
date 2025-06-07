namespace ManagedFileService.Domain.Interfaces;

public interface IAttachmentRepository
{
    Task<Attachment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Attachment> AddAsync(Attachment attachment, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Attachment>> GetAllAsync(int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Attachment>> GetByApplicationIdAsync(Guid applicationId, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<long> GetTotalStorageBytesAsync(CancellationToken cancellationToken = default);
    Task<long> GetStorageBytesForApplicationAsync(Guid applicationId, CancellationToken cancellationToken = default);
    Task<Dictionary<Guid, long>> GetStorageByApplicationAsync(CancellationToken cancellationToken = default);
    Task<int> GetTotalCountForApplicationAsync(Guid applicationId, CancellationToken cancellationToken = default);
}