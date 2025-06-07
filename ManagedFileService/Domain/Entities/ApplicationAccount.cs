namespace ManagedFileService.Domain.Entities;

public class ApplicationAccount
{
    public Guid Id { get; private set; }
    public Guid ApplicationId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string? ExternalId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    
    // Navigation property
    public AllowedApplication? Application { get; private set; }
    
    // Required for EF Core
    private ApplicationAccount() { }
    
    public ApplicationAccount(Guid applicationId, string name, string email, string? externalId = null)
    {
        Id = Guid.NewGuid();
        SetApplicationId(applicationId);
        SetName(name);
        SetEmail(email);
        SetExternalId(externalId);
        CreatedAtUtc = DateTime.UtcNow;
    }
    
    public void SetApplicationId(Guid applicationId)
    {
        if (applicationId == Guid.Empty)
            throw new ArgumentException("Application ID cannot be empty", nameof(applicationId));
        
        ApplicationId = applicationId;
    }
    
    public void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Account name cannot be empty", nameof(name));
        
        Name = name;
    }
    
    public void SetEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty", nameof(email));
        
        // Basic email validation - consider using a more robust validation if needed
        if (!email.Contains('@') || !email.Contains('.'))
            throw new ArgumentException("Invalid email format", nameof(email));
        
        Email = email;
    }
    
    public void SetExternalId(string? externalId)
    {
        ExternalId = externalId;
    }
}
