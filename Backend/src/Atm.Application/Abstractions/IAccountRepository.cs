using Atm.Domain.Accounts;

namespace Atm.Application.Abstractions;

public interface IAccountRepository
{
    /// <summary>Read-only snapshot of all accounts (not tracked for changes).</summary>
    Task<IReadOnlyList<Account>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>Loads an account for modification (tracked by the unit of work).</summary>
    Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Newest first.</summary>
    Task<(IReadOnlyList<Transaction> Items, int TotalCount)> GetTransactionsAsync(
        Guid accountId, int page, int pageSize, CancellationToken cancellationToken);
}
