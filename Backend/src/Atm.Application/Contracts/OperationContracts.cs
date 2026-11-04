namespace Atm.Application.Contracts;

public sealed record DepositRequest(decimal Amount);

public sealed record WithdrawRequest(decimal Amount);

public sealed record TransferRequest(Guid FromAccountId, Guid ToAccountId, decimal Amount);

public sealed record OperationResultDto(AccountDto Account, TransactionDto Transaction);

public sealed record TransferResultDto(
    Guid TransferId,
    AccountDto FromAccount,
    AccountDto ToAccount,
    TransactionDto Debit,
    TransactionDto Credit);

public sealed record AtmSettingsDto(decimal MaxTransactionAmount, int WithdrawalMultiple);
