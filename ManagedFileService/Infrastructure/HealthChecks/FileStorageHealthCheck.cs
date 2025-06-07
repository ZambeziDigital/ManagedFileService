// filepath: ManagedFileService/Infrastructure/HealthChecks/FileStorageHealthCheck.cs
using System.Threading;
using System.Threading.Tasks;
using ManagedFileService.Application.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ManagedFileService.Infrastructure.HealthChecks
{
    public class FileStorageHealthCheck : IHealthCheck
    {
        private readonly IFileStorageService _fileStorageService;

        public FileStorageHealthCheck(IFileStorageService fileStorageService)
        {
            _fileStorageService = fileStorageService;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            var isHealthy = await _fileStorageService.CheckHealthAsync(cancellationToken);
            if (isHealthy)
            {
                return HealthCheckResult.Healthy("File storage is healthy.");
            }

            return HealthCheckResult.Unhealthy("File storage is unhealthy.");
        }
    }
}