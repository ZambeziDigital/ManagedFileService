using ManagedFileService.Data;
using ManagedFileService.Domain.Entities;
using ManagedFileService.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ManagedFileService.Infrastructure.Persistence.Repositories;

public class ApplicationAccountRepository : IApplicationAccountRepository
{
    private readonly AppDbContext _context;
    private readonly ILogger<ApplicationAccountRepository> _logger;

    public ApplicationAccountRepository(AppDbContext context, ILogger<ApplicationAccountRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ApplicationAccount> AddAsync(ApplicationAccount account, CancellationToken cancellationToken = default)
    {
        if (account == null) throw new ArgumentNullException(nameof(account));

        await _context.ApplicationAccounts.AddAsync(account, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Added ApplicationAccount with ID: {AccountId} for Application {ApplicationId}", 
            account.Id, account.ApplicationId);
        
        return account;
    }

    public async Task<ApplicationAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var account = await _context.ApplicationAccounts
            .Include(a => a.Application)  // Include related application
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (account == null)
        {
            _logger.LogInformation("ApplicationAccount with ID: {AccountId} not found.", id);
        }

        return account;
    }

    public async Task<IReadOnlyList<ApplicationAccount>> GetByApplicationIdAsync(Guid applicationId, CancellationToken cancellationToken = default)
    {
        var accounts = await _context.ApplicationAccounts
            .AsNoTracking()
            .Where(a => a.ApplicationId == applicationId)
            .OrderBy(a => a.Name)
            .ToListAsync(cancellationToken);

        return accounts;
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ApplicationAccounts
            .AnyAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<bool> BelongsToApplicationAsync(Guid id, Guid applicationId, CancellationToken cancellationToken = default)
    {
        return await _context.ApplicationAccounts
            .AnyAsync(a => a.Id == id && a.ApplicationId == applicationId, cancellationToken);
    }
}
