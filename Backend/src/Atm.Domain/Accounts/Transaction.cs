namespace Atm.Domain.Accounts;

/// <summary>
/// An immutable ledger entry. Created only by <see cref="Account"/>, so every balance change
/// is recorded together with the resulting balance.
/// </summary>
public sealed class Transaction
{
    // Required by EF Core.
    private Transaction()
    {
        Description = string.Empty;
    }

    internal Transaction(
        Guid accountId,
        TransactionType type,
        decimal amount,
        decimal balanceAfter,
        DateTime createdAtUtc,
        string description,
        Guid? transferId,
        Guid? counterpartyAccountId)
    {
        AccountId = accountId;
        Type = type;
        Amount = amount;
        BalanceAfter = balanceAfter;
        CreatedAtUtc = createdAtUtc;
        Description = description;
        TransferId = transferId;
        CounterpartyAccountId = counterpartyAccountId;
    }

    /// <summary>Database-generated and monotonically increasing; also used as the receipt number.</summary>
    public long Id { get; private set; }
    public Guid AccountId { get; private set; }
    public TransactionType Type { get; private set; }

    /// <summary>Always positive; the direction is given by <see cref="Type"/>.</summary>
    public decimal Amount { get; private set; }
    public decimal BalanceAfter { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public string Description { get; private set; }

    /// <summary>Shared by both legs of a transfer.</summary>
    public Guid? TransferId { get; private set; }
    public Guid? CounterpartyAccountId { get; private set; }
}
