using Atm.Application.Abstractions;
using Atm.Application.Contracts;
using Atm.Application.Exceptions;
using Atm.Application.Mapping;
using FluentValidation;
using FluentValidation.Results;

namespace Atm.Application.Accounts;

public sealed class AccountQueryService(IAccountRepository accounts)
{
    public const int MaxPageSize = 100;

    public async Task<IReadOnlyList<AccountDto>> GetAccountsAsync(CancellationToken cancellationToken)
    {
        var all = await accounts.GetAllAsync(cancellationToken);
        return all.Select(a => a.ToDto()).ToList();
    }

    public async Task<AccountDto> GetAccountAsync(Guid id, CancellationToken cancellationToken)
    {
        var account = await accounts.GetByIdAsync(id, cancellationToken) ?? throw NotFoundException.Account(id);
        return account.ToDto();
    }

    public async Task<PagedResult<TransactionDto>> GetTransactionsAsync(
        Guid accountId, int page, int pageSize, CancellationToken cancellationToken)
    {
        EnsureValidPaging(page, pageSize);

        if (!await accounts.ExistsAsync(accountId, cancellationToken))
        {
            throw NotFoundException.Account(accountId);
        }

        var (items, totalCount) = await accounts.GetTransactionsAsync(accountId, page, pageSize, cancellationToken);
        return new PagedResult<TransactionDto>(items.Select(t => t.ToDto()).ToList(), page, pageSize, totalCount);
    }

    private static void EnsureValidPaging(int page, int pageSize)
    {
        var failures = new List<ValidationFailure>();
        if (page < 1)
        {
            failures.Add(new ValidationFailure("page", "Page must be 1 or greater."));
        }

        if (pageSize is < 1 or > MaxPageSize)
        {
            failures.Add(new ValidationFailure("pageSize", $"Page size must be between 1 and {MaxPageSize}."));
        }

        if (failures.Count > 0)
        {
            throw new ValidationException(failures);
        }
    }
}
