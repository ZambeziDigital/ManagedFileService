namespace ManagedFileService.Application.Interfaces;

public interface IFileStorageService
{
    // Returns the stored path/identifier
    Task<string> SaveAsync(Stream stream, string fileName, string contentType, CancellationToken cancellationToken = default);
    Task<Stream?> GetAsync(string storedPath, CancellationToken cancellationToken = default);
    Task DeleteAsync(string storedPath, CancellationToken cancellationToken = default);
    string GenerateUniqueFileName(string originalFileName);
    
    /// <summary>
    /// Checks the health and connectivity of the storage service.
    /// </summary>
    /// <returns>True if the storage service is healthy, false otherwise.</returns>
    Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default);
}