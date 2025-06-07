// using Azure.Storage.Blobs;
// using Microsoft.Extensions.Options;
// using ManagedFileService.Application.Interfaces;
//
// namespace ManagedFileService.Infrastructure.FileStorage;
//
// public class AzureBlobStorageService : IFileStorageService
// {
//     private readonly BlobServiceClient _blobServiceClient;
//     private readonly string _containerName;
//     private readonly ILogger<AzureBlobStorageService> _logger;
//
//     public AzureBlobStorageService(
//         IOptions<AzureBlobStorageOptions> options,
//         ILogger<AzureBlobStorageService> logger)
//     {
//         var config = options.Value;
//         _blobServiceClient = new BlobServiceClient(config.ConnectionString);
//         _containerName = config.ContainerName;
//         _logger = logger;
//         
//         // Ensure container exists
//         var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
//         containerClient.CreateIfNotExists();
//     }
//
//     public async Task<string> SaveFileAsync(Stream fileStream, string fileName, string contentType)
//     {
//         try
//         {
//             var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
//             var blobClient = containerClient.GetBlobClient(fileName);
//             
//             // Upload the file with content type
//             await blobClient.UploadAsync(fileStream, new BlobHttpHeaders { ContentType = contentType });
//             
//             return fileName; // Return the blob name/path
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError(ex, "Failed to save file to Azure Blob Storage: {FileName}", fileName);
//             throw new InfrastructureException("Failed to store file in cloud storage", ex);
//         }
//     }
//
//     public async Task<(Stream FileStream, string ContentType)> GetFileAsync(string filePath)
//     {
//         try
//         {
//             var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
//             var blobClient = containerClient.GetBlobClient(filePath);
//             
//             var response = await blobClient.DownloadAsync();
//             return (response.Value.Content, response.Value.ContentType);
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError(ex, "Failed to retrieve file from Azure Blob Storage: {FilePath}", filePath);
//             throw new InfrastructureException("Failed to retrieve file from cloud storage", ex);
//         }
//     }
//
//     public async Task DeleteFileAsync(string filePath)
//     {
//         try
//         {
//             var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
//             var blobClient = containerClient.GetBlobClient(filePath);
//             await blobClient.DeleteIfExistsAsync();
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError(ex, "Failed to delete file from Azure Blob Storage: {FilePath}", filePath);
//             throw new InfrastructureException("Failed to delete file from cloud storage", ex);
//         }
//     }
// }
