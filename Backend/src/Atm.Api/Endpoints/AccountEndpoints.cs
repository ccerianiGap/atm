using Atm.Application.Accounts;
using Atm.Application.Contracts;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Atm.Api.Endpoints;

internal static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/accounts").WithTags("Accounts");

        group.MapGet("/", GetAccounts).WithName("GetAccounts");
        group.MapGet("/{id:guid}", GetAccount).WithName("GetAccount");
        group.MapGet("/{id:guid}/transactions", GetTransactions).WithName("GetTransactions");
        group.MapPost("/{id:guid}/deposits", Deposit).WithName("Deposit");
        group.MapPost("/{id:guid}/withdrawals", Withdraw).WithName("Withdraw");

        return app;
    }

    private static async Task<Ok<IReadOnlyList<AccountDto>>> GetAccounts(
        AccountQueryService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.GetAccountsAsync(cancellationToken));

    private static async Task<Ok<AccountDto>> GetAccount(
        Guid id, AccountQueryService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.GetAccountAsync(id, cancellationToken));

    private static async Task<Ok<PagedResult<TransactionDto>>> GetTransactions(
        Guid id, AccountQueryService service, CancellationToken cancellationToken, int page = 1, int pageSize = 20) =>
        TypedResults.Ok(await service.GetTransactionsAsync(id, page, pageSize, cancellationToken));

    private static async Task<Ok<OperationResultDto>> Deposit(
        Guid id, DepositRequest request, AccountCommandService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.DepositAsync(id, request, cancellationToken));

    private static async Task<Ok<OperationResultDto>> Withdraw(
        Guid id, WithdrawRequest request, AccountCommandService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.WithdrawAsync(id, request, cancellationToken));
}
