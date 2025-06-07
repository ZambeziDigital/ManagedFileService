using ManagedFileService.Data;
using ManagedFileService.Domain.Entities;
using ManagedFileService.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ManagedFileService.Infrastructure.Persistence.Repositories;

public class AttachmentRepository : IAttachmentRepository
{
    private readonly AppDbContext _context;
    private readonly ILogger<AttachmentRepository> _logger; // Optional

    public AttachmentRepository(AppDbContext context, ILogger<AttachmentRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Attachment> AddAsync(Attachment attachment, CancellationToken cancellationToken = default)
    {
        if (attachment == null) throw new ArgumentNullException(nameof(attachment));

        await _context.Attachments.AddAsync(attachment, cancellationToken);
        // Note: SaveChangesAsync should ideally be handled by a Unit of Work pattern
        // or at the end of the Application layer handler for transactional consistency.
        // However, for simplicity in this direct repository implementation, we save here.
        // Consider refactoring if complex multi-step operations are needed.
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Added Attachment with ID: {AttachmentId}", attachment.Id);
        return attachment;
    }
    

    public async Task<Attachment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // FindAsync is efficient for primary key lookups
        var attachment = await _context.Attachments.FindAsync(new object[] { id }, cancellationToken: cancellationToken);

        if (attachment == null)
        {
             _logger.LogInformation("Attachment with ID: {AttachmentId} not found.", id);
        }

        return attachment;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        
        // if (attachment == null) throw new ArgumentNullException(nameof(attachment));

        // Check if the entity is already tracked; if not, attach it first.
        var attachment = await _context.Attachments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (_context.Entry(attachment).State == EntityState.Detached)
        {
             _logger.LogWarning("Attempting to delete a detached Attachment entity with ID: {AttachmentId}. Attaching first.", attachment.Id);
            _context.Attachments.Attach(attachment);
        }

        _context.Attachments.Remove(attachment);
        // See note on SaveChangesAsync in AddAsync method regarding Unit of Work.
        await _context.SaveChangesAsync(cancellationToken);
         _logger.LogInformation("Deleted Attachment with ID: {AttachmentId}", attachment.Id);
    }

    public async Task<IReadOnlyList<Attachment>> GetAllAsync(int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var attachments = await _context.Attachments
            .AsNoTracking()
            .OrderByDescending(a => a.UploadedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
            
        return attachments;
    }
    
    public async Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Attachments.CountAsync(cancellationToken);
    }
    
    public async Task<IReadOnlyList<Attachment>> GetByApplicationIdAsync(Guid applicationId, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var attachments = await _context.Attachments
            .AsNoTracking()
            .Where(a => a.ApplicationId == applicationId)
            .OrderByDescending(a => a.UploadedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
            
        return attachments;
    }
    
    public async Task<long> GetTotalStorageBytesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Attachments
            .SumAsync(a => a.SizeBytes, cancellationToken);
    }
    
    public async Task<Dictionary<Guid, long>> GetStorageByApplicationAsync(CancellationToken cancellationToken = default)
    {
        var result = await _context.Attachments
            .AsNoTracking()
            .GroupBy(a => a.ApplicationId)
            .Select(g => new 
            {
                ApplicationId = g.Key,
                TotalSize = g.Sum(a => a.SizeBytes)
            })
            .ToListAsync(cancellationToken);
            
        return result.ToDictionary(r => r.ApplicationId, r => r.TotalSize);
    }

    public async Task<int> GetTotalCountForApplicationAsync(Guid applicationId, CancellationToken cancellationToken = default)
    {
        return await _context.Attachments
            .AsNoTracking()
            .CountAsync(a => a.ApplicationId == applicationId, cancellationToken);
    }

    public async Task<long> GetStorageBytesForAttachmentAsync(Guid attachmentId, CancellationToken cancellationToken = default)
    {
        var attachment = await _context.Attachments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == attachmentId, cancellationToken);
            
        return attachment?.SizeBytes ?? 0;
    }
    public async Task<long> GetStorageBytesForApplicationAsync(Guid applicationId, CancellationToken cancellationToken = default)
    {
        return await _context.Attachments
            .Where(a => a.ApplicationId == applicationId)
            .SumAsync(a => a.SizeBytes, cancellationToken);
    }
}