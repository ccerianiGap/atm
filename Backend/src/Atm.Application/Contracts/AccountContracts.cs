namespace Atm.Application.Contracts;

public sealed record AccountDto(Guid Id, string AccountNumber, string Name, decimal Balance);

public sealed record TransactionDto(
    long Id,
    Guid AccountId,
    string Type,
    decimal Amount,
    decimal BalanceAfter,
    DateTime CreatedAtUtc,
    string Description,
    Guid? TransferId,
    Guid? CounterpartyAccountId);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
