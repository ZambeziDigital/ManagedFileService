global using ManagedFileService.Domain.Entities;

namespace ManagedFileService.Domain.Interfaces;

public interface IAllowedApplicationRepository
{
    Task<AllowedApplication?> FindByApiKeyAsync(string apiKey, CancellationToken cancellationToken = default);
    Task<AllowedApplication?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(AllowedApplication application, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AllowedApplication>> GetAllAsync(CancellationToken cancellationToken = default);
    Task UpdateAsync(AllowedApplication application, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AllowedApplication>> GetAdminApplicationsAsync(CancellationToken cancellationToken = default);
}