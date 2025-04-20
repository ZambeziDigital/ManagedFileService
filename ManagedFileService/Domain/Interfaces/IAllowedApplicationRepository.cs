global using ManagedFileService.Domain.Entities;

namespace ManagedFileService.Domain.Interfaces;

public interface IAllowedApplicationRepository
{
    Task<AllowedApplication?> FindByApiKeyAsync(string apiKey, CancellationToken cancellationToken = default);
    Task<AllowedApplication?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(AllowedApplication application, CancellationToken cancellationToken = default); // <-- Add this method
    // Potentially add: Task<AllowedApplication?> FindByNameAsync(string name, CancellationToken cancellationToken = default);
}