namespace ManagedFileService.Infrastructure.Services;

public class SignedUrlSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public int? MaxExpiryMinutes { get; set; } // Nullable if no max limit
}