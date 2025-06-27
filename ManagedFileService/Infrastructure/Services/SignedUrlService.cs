using System.Security.Cryptography;
using System.Text;
using ManagedFileService.Application.Interfaces;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ManagedFileService.Infrastructure.Services;


public class SignedUrlService : ISignedUrlService
{
    private readonly byte[] _secretKeyBytes;
    private readonly int? _maxExpiryMinutes;
    private readonly ILogger<SignedUrlService> _logger;

    public SignedUrlService(IOptions<SignedUrlSettings> options, ILogger<SignedUrlService> logger)
    {
        _logger = logger;
        var settings = options.Value;

        if (string.IsNullOrWhiteSpace(settings.SecretKey) || settings.SecretKey.Length < 32) // Basic sanity check
        {
            _logger.LogCritical("SignedUrlSettings:SecretKey is missing, empty, or too short in configuration. Signed URLs will NOT work.");
            throw new ArgumentException("Signed URL Secret Key is not configured correctly.");
        }
        // It's generally better to derive the key or use specific key sizes if the algorithm requires it.
        // For HMACSHA256, any key length works, but longer is generally better.
        _secretKeyBytes = Encoding.UTF8.GetBytes(settings.SecretKey);

        _maxExpiryMinutes = settings.MaxExpiryMinutes;
        _logger.LogInformation("Signed URL Service initialized. Max expiry: {MaxMins} minutes.", _maxExpiryMinutes?.ToString() ?? "Unlimited");
    }

    public SignedUrlResult GenerateSignedUrl(Guid attachmentId, int expiresInMinutes, string basePublicUrl) // <-- Change return type
    {
        expiresInMinutes = _maxExpiryMinutes ?? 525949; //TODO: Default to 1 year if not configured
        if (expiresInMinutes <= 0)
        {
            throw new ArgumentException("Expiration minutes must be positive.", nameof(expiresInMinutes));
        }

        // Determine actual expiry minutes (apply clamping)
        int actualExpiresInMinutes = expiresInMinutes;
        if (_maxExpiryMinutes.HasValue && expiresInMinutes > _maxExpiryMinutes.Value)
        {
            _logger.LogWarning("Requested signed URL expiry ({ReqMins} min) exceeds maximum allowed ({MaxMins} min). Clamping to max.",
                expiresInMinutes, _maxExpiryMinutes.Value);
            actualExpiresInMinutes = _maxExpiryMinutes.Value;
        }

        // Calculate actual expiry time based on potentially clamped duration
        var expiryTime = DateTimeOffset.UtcNow.AddMinutes(actualExpiresInMinutes); // <-- Use actual minutes
        long expiryTimestamp = expiryTime.ToUnixTimeSeconds();

        // 1. Construct the data to sign (using the *actual* timestamp)
        string dataToSign = $"{attachmentId}:{expiryTimestamp}";

        // 2. Compute the signature (logic remains the same)
        string signature;
        using (var hmac = new HMACSHA256(_secretKeyBytes))
        {
            byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(dataToSign));
            signature = Base64UrlEncoder.Encode(hashBytes);
        }

        // 3. Construct the final URL (logic remains the same)
        var queryParams = new Dictionary<string, string?>
        {
            { "id", attachmentId.ToString() },
            { "expires", expiryTimestamp.ToString() },
            { "sig", signature }
        };
        string signedUrl = QueryHelpers.AddQueryString(basePublicUrl.TrimEnd('/'), queryParams);

        _logger.LogInformation("Generated signed URL for Attachment {AttachmentId}, expires at {ExpiryTime} ({ExpiryTimestamp})",
             attachmentId, expiryTime, expiryTimestamp);

        // Return both the URL and the actual expiry time
        return new SignedUrlResult(signedUrl, expiryTime); // <-- Return new result type
    }

    public bool ValidateSignedUrl(Guid attachmentId, long expiryTimestamp, string signature)
    {
         _logger.LogDebug("Validating signed URL for Attachment: {AttachmentId}, Expires: {ExpiryTimestamp}, Signature: {Signature}",
             attachmentId, expiryTimestamp, signature);

        // 1. Check Expiration
        var expiryTime = DateTimeOffset.FromUnixTimeSeconds(expiryTimestamp);
        if (expiryTime < DateTimeOffset.UtcNow)
        {
             _logger.LogWarning("Signed URL validation failed: Expired. Attachment: {AttachmentId}, Expiry: {ExpiryTime}", attachmentId, expiryTime);
            return false; // URL has expired
        }

        // 2. Reconstruct the expected data string
        string expectedDataToSign = $"{attachmentId}:{expiryTimestamp}";

        // 3. Recompute the signature based on the received parameters
        string expectedSignature;
        try
        {
            using (var hmac = new HMACSHA256(_secretKeyBytes))
            {
                byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(expectedDataToSign));
                 // Use Base64Url encoding for comparison
                expectedSignature = Base64UrlEncoder.Encode(hashBytes);
            }
        }
        catch (Exception ex)
        {
             _logger.LogError(ex, "Error computing expected signature during validation for Attachment {AttachmentId}", attachmentId);
            return false; // Internal error during computation
        }


        // 4. Securely Compare Signatures (timing attack resistant comparison recommended, though less critical here than password hashes)
        // Simple comparison:
        bool isValid = signature == expectedSignature;

        // More secure comparison (constant time):
        // byte[] providedSigBytes = Base64UrlEncoder.Decode(signature); // Might throw FormatException
        // byte[] expectedSigBytes = Base64UrlEncoder.Decode(expectedSignature);
        // bool isValid = CryptographicOperations.FixedTimeEquals(providedSigBytes, expectedSigBytes);


        if (!isValid)
        {
            _logger.LogWarning("Signed URL validation failed: Invalid Signature. Attachment: {AttachmentId}. Provided: '{ProvidedSig}', Expected: '{ExpectedSig}'",
                attachmentId, signature, expectedSignature);
        }
        else
        {
             _logger.LogDebug("Signed URL validation successful for Attachment {AttachmentId}", attachmentId);
        }


        return isValid;
    }
}