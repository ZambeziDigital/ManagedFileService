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
builder.Services.Configure<SignedUrlSettings>(builder.Configuration.GetSection("SignedUrlSettings"));
// File Storage (Local example)
builder.Services.Configure<FileStorageOptions>(builder.Configuration.GetSection("FileStorage"));
builder.Services.AddSingleton<IFileStorageService, LocalFileStorageService>(); // Singleton might be okay for local
// --- Service Registration ---
builder.Services.AddSingleton<ISignedUrlService, SignedUrlService>(); // Singleton is suitable if state relies only on config

// Current Request Service
builder.Services.AddHttpContextAccessor(); // Required for HttpContext access
builder.Services.AddScoped<ICurrentRequestService, CurrentRequestService>();


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
    // await dbContext.Database.EnsureCreatedAsync(); // Or use MigrateAsync
    await dbContext.Database.MigrateAsync();

    // Seed initial data if needed (e.g., a default application for testing)
    // await SeedData.InitializeAsync(scope.ServiceProvider);
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
// Note: No app.UseAuthentication() or app.UseAuthorization() needed if ONLY using the API key middleware
// If you mix authentication schemes, you'd need them.

app.MapControllers();
app.UseRouting();

// --- Custom API Key Middleware ---
app.UseMiddleware<ApiKeyAuthMiddleware>(); // Add custom middleware BEFORE Authorization/Controllers



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