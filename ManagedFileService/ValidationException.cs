namespace ManagedFileService;

/// <summary>
/// Exception thrown when validation fails for a request.
/// </summary>
public class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
    
    public ValidationException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when a user attempts to access a resource they don't have permission for.
/// </summary>
public class ForbiddenAccessException : UnauthorizedAccessException
{
    public ForbiddenAccessException(string message) : base(message) { }
    
    public ForbiddenAccessException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when an infrastructure component fails.
/// </summary>
public class InfrastructureException : Exception
{
    public InfrastructureException(string message) : base(message) { }
    
    public InfrastructureException(string message, Exception innerException) : base(message, innerException) { }
}
