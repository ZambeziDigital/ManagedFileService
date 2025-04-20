using ManagedFileService.Application.Interfaces;

namespace ManagedFileService.Infrastructure.Services;


public class CurrentRequestService : ICurrentRequestService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    // Define a constant for the key used in HttpContext.Items by the middleware
    private const string ApplicationContextItemKey = "AllowedApplication"; // MUST match the key used in ApiKeyAuthMiddleware

    public CurrentRequestService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    private HttpContext? CurrentHttpContext => _httpContextAccessor.HttpContext;

    /// <summary>
    /// Gets the full AllowedApplication entity stored in HttpContext.Items by the authentication middleware.
    /// </summary>
    /// <returns>The AllowedApplication entity.</returns>
    /// <exception cref="InvalidOperationException">Thrown if HttpContext or the application item is not available.</exception>
    public AllowedApplication GetApplication()
    {
        if (CurrentHttpContext == null)
        {
            // This typically means the service is being called outside the context of an HTTP request
            // (e.g., background job, unit test without mock).
            throw new InvalidOperationException("HttpContext is not available in the current scope.");
        }

        // Retrieve the application object stored by the middleware
        if (CurrentHttpContext.Items.TryGetValue(ApplicationContextItemKey, out var appObject) &&
            appObject is AllowedApplication application)
        {
            return application;
        }

        // If using ClaimsPrincipal approach in middleware instead of HttpContext.Items:
        /*
        var principal = CurrentHttpContext.User;
        if (principal?.Identity?.IsAuthenticated == true)
        {
            // Assuming you store the AppId and potentially other details as claims
            var appIdClaim = principal.FindFirst("AppId"); // Use the ClaimTypes constant or string used in middleware
            if (appIdClaim != null && Guid.TryParse(appIdClaim.Value, out Guid appId))
            {
                // PROBLEM: This doesn't easily give you the *full* AllowedApplication object
                // with configuration (like MaxFileSize). You would need to inject
                // IAllowedApplicationRepository here and fetch it again based on the ID claim,
                // which might be less efficient than storing the object in HttpContext.Items.
                // For simplicity, sticking with HttpContext.Items is often easier for this pattern.

                 // Fetching example (requires injecting IAllowedApplicationRepository):
                 // var app = await _appRepository.GetByIdAsync(appId); // Needs async modifier on method + repo injection
                 // if (app == null) throw new InvalidOperationException($"Application details not found for authenticated ID '{appId}'.");
                 // return app;
            }
        }
        */


        // If we reach here, the application object wasn't found where expected.
        throw new InvalidOperationException($"Authenticated application details ('{ApplicationContextItemKey}') not found in HttpContext.Items. Ensure authentication middleware runs successfully and populates the context.");
    }

    /// <summary>
    /// Gets the unique identifier of the currently authenticated application by retrieving the full entity first.
    /// </summary>
    /// <returns>The Guid of the application.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the application entity or ID cannot be determined.</exception>
    public Guid GetApplicationId()
    {
        // Get the full application object first, which handles the null/missing checks.
        var application = GetApplication();
        return application.Id; // Simply return the ID from the retrieved object
    }
}