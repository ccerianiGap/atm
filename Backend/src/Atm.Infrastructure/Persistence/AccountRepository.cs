using Atm.Application.Abstractions;
using Atm.Domain.Accounts;
using Microsoft.EntityFrameworkCore;

namespace Atm.Infrastructure.Persistence;

internal sealed class AccountRepository(AtmDbContext db) : IAccountRepository
{
    public async Task<IReadOnlyList<Account>> GetAllAsync(CancellationToken cancellationToken) =>
        await db.Accounts
            .AsNoTracking()
            .OrderBy(a => a.AccountNumber)
            .ToListAsync(cancellationToken);

    public Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Accounts.SingleOrDefaultAsync(a => a.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken) =>
        db.Accounts.AnyAsync(a => a.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<Transaction> Items, int TotalCount)> GetTransactionsAsync(
        Guid accountId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = db.Transactions.AsNoTracking().Where(t => t.AccountId == accountId);

        var totalCount = await query.CountAsync(cancellationToken);

        // Id is monotonically increasing, so it breaks ties between entries with the same timestamp.
        var items = await query
            .OrderByDescending(t => t.CreatedAtUtc)
            .ThenByDescending(t => t.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
