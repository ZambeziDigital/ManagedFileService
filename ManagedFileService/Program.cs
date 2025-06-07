using ManagedFileService;
using ManagedFileService.Application.Interfaces;
using ManagedFileService.Data;
using ManagedFileService.Domain.Interfaces;
using ManagedFileService.Infrastructure.FileStorage;
using ManagedFileService.Infrastructure.Persistence.Repositories;
using ManagedFileService.Infrastructure.Services;
using ManagedFileService.Middleware;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using ManagedFileService.Infrastructure.HealthChecks; // Add this using
using HealthChecks.UI.Client; // Add this for UIResponseWriter if you want a detailed UI
using Microsoft.AspNetCore.Diagnostics.HealthChecks; // Add this for HealthCheckOptions

var builder = WebApplication.CreateBuilder(args);

// --- Configuration ---
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();

// --- Dependency Injection ---

// Application Layer
builder.Services.AddApplicationServices(); // Extension method for MediatR, AutoMapper etc.



// Infrastructure Layer
// Persistence (PostgreSQL example)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))); // Get from config

builder.Services.AddScoped<IAttachmentRepository, AttachmentRepository>();
builder.Services.AddScoped<IAllowedApplicationRepository, AllowedApplicationRepository>();
builder.Services.AddScoped<IApplicationAccountRepository, ApplicationAccountRepository>();
builder.Services.Configure<SignedUrlSettings>(builder.Configuration.GetSection("SignedUrlSettings"));
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>(); // Ensure IFileStorageService is registered
// Add ZIP archive service
builder.Services.AddScoped<IZipArchiveService, ZipArchiveService>();
// Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("Database") // Checks database connectivity
    .AddCheck<FileStorageHealthCheck>("FileStorage"); // Custom check for file storage

// File Storage Configuration
var storageProvider = builder.Configuration.GetValue<string>("FileStorage");
// if (storageProvider == "AzureBlob")
// {
//     builder.Services.Configure<AzureBlobStorageOptions>(builder.Configuration.GetSection("FileStorage:Azure"));
//     builder.Services.AddSingleton<IFileStorageService, AzureBlobStorageService>();
// }
// else if (storageProvider == "AwsS3")
// {
//     builder.Services.Configure<AwsS3Options>(builder.Configuration.GetSection("FileStorage:AwsS3"));
//     builder.Services.AddSingleton<IFileStorageService, AwsS3StorageService>();
// }
// else // Default to local storage
{
    builder.Services.Configure<FileStorageOptions>(builder.Configuration.GetSection("FileStorage"));
    builder.Services.AddSingleton<IFileStorageService, LocalFileStorageService>();
}
// --- Service Registration ---
builder.Services.AddSingleton<ISignedUrlService, SignedUrlService>(); // Singleton is suitable if state relies only on config

// Current Request Service
builder.Services.AddHttpContextAccessor(); // Required for HttpContext access
builder.Services.AddScoped<ICurrentRequestService, CurrentRequestService>();
// add CORS policy for Wasm client
builder.Services.AddCors(
    options => options.AddPolicy(
        "wasm",
        policy => policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .SetIsOriginAllowed(pol => true)
            .AllowAnyHeader()
            // .AllowCredentials()
        ));

// API Layer Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
// builder.Services.AddSwaggerGen(options => // Add Swagger for testing
// {
//     options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "Attachment Service API", Version = "v1" });
//     // Add support for API Key in Swagger UI
//     options.AddSecurityDefinition("ApiKey", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
//     {
//         In = Microsoft.OpenApi.Models.ParameterLocation.Header,
//         Name = "X-Api-Key",
//         Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
//         Description = "API Key needed to access the endpoints."
//     });
//     options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
//     {
//         {
//             new Microsoft.OpenApi.Models.OpenApiSecurityScheme
//             {
//                 Reference = new Microsoft.OpenApi.Models.OpenApiReference
//                 {
//                     Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
//                     Id = "ApiKey"
//                 }
//             },
//             Array.Empty<string>()
//         }
//     });
// });


var app = builder.Build();

// Configure the HTTP request pipeline.
// if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    
    app.MapScalarApiReference();
}

// --- Database Migration ---
// Apply migrations automatically on startup (convenient for dev, consider manual for prod)
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();

    // Seed initial data if needed
    if (app.Environment.IsDevelopment())
    {
        await SeedData.InitializeAsync(app.Services);
    }
}


// --- Middleware Pipeline ---
if (app.Environment.IsDevelopment())
{
    // app.UseSwagger();
    // app.UseSwaggerUI();
    app.UseDeveloperExceptionPage(); // More detail in dev
}
else
{
     app.UseExceptionHandler("/error"); // Configure proper error handling
     app.UseHsts();
}

// app.UseHttpsRedirection();
app.UseRouting(); // UseRouting should come BEFORE middleware that depends on route information

// --- Custom API Key Middleware ---
app.UseMiddleware<ApiKeyAuthMiddleware>(); // Add custom middleware AFTER UseRouting

app.UseCors("wasm");
// app.UseAuthorization(); // Uncomment if needed
app.MapControllers();

// It will return a 200 status if all checks are healthy, otherwise 503.
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true, // Include all checks
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse // Provides a detailed JSON response
});

app.Run();

// --- Helper Extensions (Example for Application Layer Services) ---
public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Add MediatR - scanning the Application assembly for handlers
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ApplicationServiceRegistration).Assembly));

        // Add AutoMapper (if using)
        // services.AddAutoMapper(typeof(ApplicationServiceRegistration).Assembly);

        // Add FluentValidation (if using)
        // services.AddValidatorsFromAssembly(typeof(ApplicationServiceRegistration).Assembly);
        // services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>)); // MediatR validation pipeline

        return services;
    }
}