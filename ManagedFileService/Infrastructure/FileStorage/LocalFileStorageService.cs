using ManagedFileService.Application.Interfaces;
using Microsoft.Extensions.Options;
using Shared.Extensions;

namespace ManagedFileService.Infrastructure.FileStorage;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _basePath;

    public LocalFileStorageService(IOptions<FileStorageOptions> options)
    {
        _basePath = options.Value.BasePath;
        if (string.IsNullOrWhiteSpace(_basePath))
        {
            throw new ArgumentException("File storage base path cannot be empty.", nameof(options));
        }
        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
        }
    }

    public string GenerateUniqueFileName(string originalFileName)
    {
        var extension = Path.GetExtension(originalFileName);
        // Ensure filename is safe for the filesystem
        var safeBaseName = Path.GetFileNameWithoutExtension(originalFileName)
                               .ReplaceInvalidFileNameChars(); // Add extension method if needed
        return $"{safeBaseName}_{Path.GetRandomFileName()}{extension}";
    }


    public async Task<string> SaveAsync(Stream stream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        // Maybe create subdirectories per application/date for organization
        var filePath = Path.Combine(_basePath, fileName);

        await using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
        {
            await stream.CopyToAsync(fileStream, cancellationToken);
        }
        return fileName; // Return the relative path/filename used for storage
    }

    public Task<Stream?> GetAsync(string storedPath, CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(_basePath, storedPath);
        if (!File.Exists(filePath))
        {
            return Task.FromResult<Stream?>(null);
        }
        // Important: The caller MUST dispose of this stream
        return Task.FromResult<Stream?>(new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read));
    }

    public Task DeleteAsync(string storedPath, CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(_basePath, storedPath);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
        // Consider what to do if file not found (log? ignore?)
        return Task.CompletedTask;
    }
}
