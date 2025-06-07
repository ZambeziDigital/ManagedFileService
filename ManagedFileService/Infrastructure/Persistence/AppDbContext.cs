using System.Reflection;
using ManagedFileService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ManagedFileService.Data;


public class AppDbContext : DbContext
{
    public DbSet<Attachment> Attachments { get; set; }
    public DbSet<AllowedApplication> AllowedApplications { get; set; }
    public DbSet<ApplicationAccount> ApplicationAccounts { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly()); // Scan for IEntityTypeConfiguration

        // Example configuration (can be in separate files)
        modelBuilder.Entity<AllowedApplication>(entity =>
        {
            entity.HasIndex(e => e.ApiKeyHash).IsUnique(); // Index for lookup
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
             entity.Property(e => e.ApiKeyHash).IsRequired();
        });

         modelBuilder.Entity<Attachment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OriginalFileName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.ContentType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.StoredPath).IsRequired().HasMaxLength(500);
            // Index on ApplicationId if querying by app is common
            entity.HasIndex(e => e.ApplicationId);
        });

        modelBuilder.Entity<ApplicationAccount>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.ExternalId).HasMaxLength(100);
            
            // Reference to parent application
            entity.HasOne(e => e.Application)
                .WithMany()
                .HasForeignKey(e => e.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Index on ApplicationId for efficient lookups
            entity.HasIndex(e => e.ApplicationId);
            
            // Index on external ID for lookups
            entity.HasIndex(e => e.ExternalId);
        });

        // modelBuilder.Entity<AllowedApplication>().HasData(
        //    
        //         new AllowedApplication
        //         (
        //             "AdminApp",
        //             "AdminApp",
        //             true,
        //         )
        //     );


        base.OnModelCreating(modelBuilder);
    }
}
