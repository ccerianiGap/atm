using Atm.Application.Abstractions;
using Atm.Application.Contracts;
using Atm.Application.Exceptions;
using Atm.Application.Mapping;
using Atm.Domain.Accounts;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Atm.Application.Accounts;

/// <summary>
/// Orchestrates the money-moving use cases: validate input, load aggregates, let the domain
/// apply the change, commit. Business rules themselves live in the domain.
/// </summary>
public sealed class AccountCommandService(
    IAccountRepository accounts,
    IUnitOfWork unitOfWork,
    IValidator<DepositRequest> depositValidator,
    IValidator<WithdrawRequest> withdrawValidator,
    IValidator<TransferRequest> transferValidator,
    TimeProvider timeProvider,
    ILogger<AccountCommandService> logger)
{
    public async Task<OperationResultDto> DepositAsync(Guid accountId, DepositRequest request, CancellationToken cancellationToken)
    {
        await depositValidator.ValidateAndThrowAsync(request, cancellationToken);

        var account = await LoadAsync(accountId, cancellationToken);
        var transaction = account.Deposit(request.Amount, UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Deposited {Amount} into account {AccountId} (transaction {TransactionId})",
            request.Amount, accountId, transaction.Id);

        return new OperationResultDto(account.ToDto(), transaction.ToDto());
    }

    public async Task<OperationResultDto> WithdrawAsync(Guid accountId, WithdrawRequest request, CancellationToken cancellationToken)
    {
        await withdrawValidator.ValidateAndThrowAsync(request, cancellationToken);

        var account = await LoadAsync(accountId, cancellationToken);
        var transaction = account.Withdraw(request.Amount, UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Withdrew {Amount} from account {AccountId} (transaction {TransactionId})",
            request.Amount, accountId, transaction.Id);

        return new OperationResultDto(account.ToDto(), transaction.ToDto());
    }

    public async Task<TransferResultDto> TransferAsync(TransferRequest request, CancellationToken cancellationToken)
    {
        await transferValidator.ValidateAndThrowAsync(request, cancellationToken);

        var source = await LoadAsync(request.FromAccountId, cancellationToken);
        var destination = await LoadAsync(request.ToAccountId, cancellationToken);

        var legs = TransferService.Transfer(source, destination, request.Amount, UtcNow);

        // Both legs and both balances are committed in a single database transaction.
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Transferred {Amount} from account {FromAccountId} to account {ToAccountId} (transfer {TransferId})",
            request.Amount, source.Id, destination.Id, legs.TransferId);

        return new TransferResultDto(
            legs.TransferId,
            source.ToDto(),
            destination.ToDto(),
            legs.Debit.ToDto(),
            legs.Credit.ToDto());
    }

    private DateTime UtcNow => timeProvider.GetUtcNow().UtcDateTime;

    private async Task<Account> LoadAsync(Guid id, CancellationToken cancellationToken) =>
        await accounts.GetByIdAsync(id, cancellationToken) ?? throw NotFoundException.Account(id);
}
