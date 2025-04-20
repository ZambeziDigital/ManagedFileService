using ManagedFileService.Data;
using ManagedFileService.Domain.Entities;
using ManagedFileService.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ManagedFileService;

public class AttachmentRepository : IAttachmentRepository
{
    private readonly AppDbContext _context;
    private readonly ILogger<AttachmentRepository> _logger; // Optional

    public AttachmentRepository(AppDbContext context, ILogger<AttachmentRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task AddAsync(Attachment attachment, CancellationToken cancellationToken = default)
    {
        if (attachment == null) throw new ArgumentNullException(nameof(attachment));

        await _context.Attachments.AddAsync(attachment, cancellationToken);
        // Note: SaveChangesAsync should ideally be handled by a Unit of Work pattern
        // or at the end of the Application layer handler for transactional consistency.
        // However, for simplicity in this direct repository implementation, we save here.
        // Consider refactoring if complex multi-step operations are needed.
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Added Attachment with ID: {AttachmentId}", attachment.Id);
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

    public async Task DeleteAsync(Attachment attachment, CancellationToken cancellationToken = default)
    {
        if (attachment == null) throw new ArgumentNullException(nameof(attachment));

        // Check if the entity is already tracked; if not, attach it first.
        var entry = _context.Entry(attachment);
        if (entry.State == EntityState.Detached)
        {
             _logger.LogWarning("Attempting to delete a detached Attachment entity with ID: {AttachmentId}. Attaching first.", attachment.Id);
            _context.Attachments.Attach(attachment);
        }

        _context.Attachments.Remove(attachment);
        // See note on SaveChangesAsync in AddAsync method regarding Unit of Work.
        await _context.SaveChangesAsync(cancellationToken);
         _logger.LogInformation("Deleted Attachment with ID: {AttachmentId}", attachment.Id);
    }

    // Add other specific query methods if needed, e.g.:
    // public async Task<IEnumerable<Attachment>> GetByApplicationIdAsync(Guid applicationId, CancellationToken cancellationToken = default)
    // {
    //     return await _context.Attachments
    //         .Where(a => a.ApplicationId == applicationId)
    //         .ToListAsync(cancellationToken);
    // }
}