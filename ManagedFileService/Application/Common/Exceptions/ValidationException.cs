namespace ManagedFileService.Application.Common.Exceptions;

public class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
    // Add constructors for dictionary of validation errors if using FluentValidation
}