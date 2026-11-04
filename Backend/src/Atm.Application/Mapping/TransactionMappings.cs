using Atm.Application.Contracts;
using Atm.Domain.Accounts;

namespace Atm.Application.Mapping;

public static class TransactionMappings
{
    public static TransactionDto ToDto(this Transaction transaction) =>
        new(
            transaction.Id,
            transaction.AccountId,
            transaction.Type.ToString(),
            transaction.Amount,
            transaction.BalanceAfter,
            transaction.CreatedAtUtc,
            transaction.Description,
            transaction.TransferId,
            transaction.CounterpartyAccountId);
}
