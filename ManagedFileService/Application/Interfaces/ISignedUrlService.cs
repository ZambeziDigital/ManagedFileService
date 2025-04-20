namespace ManagedFileService.Application.Interfaces;

public interface ISignedUrlService
{
    /// <summary>
    /// Generates a signed URL for downloading a specific attachment.
    /// </summary>
    /// <param name="attachmentId">The ID of the attachment.</param>
    /// <param name="expiresInMinutes">How long the URL should be valid in minutes.</param>
    /// <param name="basePublicUrl">The base URL for the public download endpoint (e.g., "https://your-service.com/api/publicdownloads/download").</param>
    /// <returns>The generated signed URL.</returns>
    /// <exception cref="ArgumentException">Thrown if expiresInMinutes exceeds configured limits.</exception>
    SignedUrlResult GenerateSignedUrl(Guid attachmentId, int expiresInMinutes, string basePublicUrl);

    /// <summary>
    /// Verifies a signed URL's signature and expiration.
    /// </summary>
    /// <param name="attachmentId">The attachment ID from the URL parameters.</param>
    /// <param name="expiryTimestamp">The expiry timestamp (Unix epoch seconds) from the URL parameters.</param>
    /// <param name="signature">The signature from the URL parameters.</param>
    /// <returns>True if the signature is valid and the URL has not expired, false otherwise.</returns>
    bool ValidateSignedUrl(Guid attachmentId, long expiryTimestamp, string signature);
}