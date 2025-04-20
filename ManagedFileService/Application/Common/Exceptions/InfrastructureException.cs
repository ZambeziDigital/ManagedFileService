namespace ManagedFileService.Application.Common.Exceptions;

public class InfrastructureException : Exception
{
    public InfrastructureException(string message, Exception? innerException = null) : base(message, innerException) { }
}