using Atm.Domain.Exceptions;

namespace Atm.Domain.Accounts;

/// <summary>
/// Aggregate root. The balance can only change through the methods below, each of which
/// enforces the account invariants and records a <see cref="Transaction"/>.
/// </summary>
public sealed class Account
{
    // Holds the transactions created in the current unit of work. The full history is
    // unbounded, so it is never loaded with the aggregate; it is queried separately.
    private readonly List<Transaction> _transactions = [];

    // Required by EF Core.
    private Account()
    {
        AccountNumber = string.Empty;
        Name = string.Empty;
    }

    private Account(Guid id, string accountNumber, string name)
    {
        Id = id;
        AccountNumber = accountNumber;
        Name = name;
    }

    public Guid Id { get; private set; }
    public string AccountNumber { get; private set; }
    public string Name { get; private set; }
    public decimal Balance { get; private set; }
    public IReadOnlyCollection<Transaction> Transactions => _transactions.AsReadOnly();

    public static Account Open(Guid id, string accountNumber, string name, decimal openingBalance, DateTime utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegative(openingBalance);

        var account = new Account(id, accountNumber, name);
        if (openingBalance > 0)
        {
            account.Credit(TransactionType.Deposit, openingBalance, utcNow, "Opening deposit");
        }

        return account;
    }

    public Transaction Deposit(decimal amount, DateTime utcNow) =>
        Credit(TransactionType.Deposit, amount, utcNow, "Cash deposit");

    public Transaction Withdraw(decimal amount, DateTime utcNow) =>
        Debit(TransactionType.Withdrawal, amount, utcNow, "Cash withdrawal");

    // Transfer legs are internal: they must be created in pairs, which TransferService guarantees.
    internal Transaction TransferOut(decimal amount, Account destination, Guid transferId, DateTime utcNow) =>
        Debit(TransactionType.TransferOut, amount, utcNow, $"Transfer to {destination.Name}", transferId, destination.Id);

    internal Transaction TransferIn(decimal amount, Account source, Guid transferId, DateTime utcNow) =>
        Credit(TransactionType.TransferIn, amount, utcNow, $"Transfer from {source.Name}", transferId, source.Id);

    private Transaction Credit(
        TransactionType type, decimal amount, DateTime utcNow, string description,
        Guid? transferId = null, Guid? counterpartyId = null)
    {
        AmountRules.EnsureValid(amount);

        Balance += amount;
        return Record(type, amount, utcNow, description, transferId, counterpartyId);
    }

    private Transaction Debit(
        TransactionType type, decimal amount, DateTime utcNow, string description,
        Guid? transferId = null, Guid? counterpartyId = null)
    {
        AmountRules.EnsureValid(amount);
        if (amount > Balance)
        {
            throw new InsufficientFundsException(Id, amount, Balance);
        }

        Balance -= amount;
        return Record(type, amount, utcNow, description, transferId, counterpartyId);
    }

    private Transaction Record(
        TransactionType type, decimal amount, DateTime utcNow, string description,
        Guid? transferId, Guid? counterpartyId)
    {
        var transaction = new Transaction(Id, type, amount, Balance, utcNow, description, transferId, counterpartyId);
        _transactions.Add(transaction);
        return transaction;
    }
}
