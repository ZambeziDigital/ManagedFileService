namespace ManagedFileService.Application.Interfaces;

public interface ICurrentRequestService
{
    Guid GetApplicationId();
    AllowedApplication GetApplication(); // Maybe cache this per request
}