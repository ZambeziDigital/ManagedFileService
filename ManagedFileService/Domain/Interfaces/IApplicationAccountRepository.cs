namespace ManagedFileService.Domain.Interfaces;

public interface IApplicationAccountRepository
{
    Task<ApplicationAccount> AddAsync(ApplicationAccount account, CancellationToken cancellationToken = default);
    Task<ApplicationAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ApplicationAccount>> GetByApplicationIdAsync(Guid applicationId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> BelongsToApplicationAsync(Guid id, Guid applicationId, CancellationToken cancellationToken = default);
}
