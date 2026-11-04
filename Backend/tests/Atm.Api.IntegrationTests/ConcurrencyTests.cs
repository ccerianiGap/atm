using System.Net;
using Atm.Application.Abstractions;
using Atm.Application.Exceptions;
using Atm.Infrastructure.Persistence;
using Atm.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Atm.Api.IntegrationTests;

public sealed class ConcurrencyTests : IDisposable
{
    private static readonly Guid Checking = DatabaseSeeder.CheckingAccountId;
    private static readonly Guid Savings = DatabaseSeeder.SavingsAccountId;
    private static readonly DateTime Now = DateTime.UtcNow;

    private readonly AtmApiFactory _factory = new();
    private readonly HttpClient _client;

    public ConcurrencyTests() => _client = _factory.CreateClient();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Second_writer_with_a_stale_account_gets_a_conflict()
    {
        // Two independent units of work load the same account version...
        using var scopeA = _factory.Services.CreateScope();
        using var scopeB = _factory.Services.CreateScope();
        var accountA = (await Repository(scopeA).GetByIdAsync(Checking, default))!;
        var accountB = (await Repository(scopeB).GetByIdAsync(Checking, default))!;

        accountA.Withdraw(600m, Now);
        accountB.Withdraw(600m, Now);

        // ...the first commit wins, the second is rejected instead of overdrawing the account.
        await UnitOfWork(scopeA).SaveChangesAsync(default);
        var act = () => UnitOfWork(scopeB).SaveChangesAsync(default);

        await act.Should().ThrowAsync<ConcurrencyConflictException>();
        (await _client.GetAccountAsync(Checking)).Balance.Should().Be(400m);
        (await _client.GetTransactionsAsync(Checking)).TotalCount.Should().Be(2, "opening deposit + one withdrawal");
    }

    [Fact]
    public async Task Each_commit_increments_the_version_of_every_modified_account()
    {
        var before = await VersionsAsync();

        await _client.TransferAsync(Checking, Savings, 10m);

        var after = await VersionsAsync();
        after[Checking].Should().Be(before[Checking] + 1);
        after[Savings].Should().Be(before[Savings] + 1);
    }

    [Fact]
    public async Task Parallel_withdrawals_never_overdraw_and_history_matches_balance()
    {
        const decimal opening = 1_000m;
        const decimal amount = 200m;

        var responses = await Task.WhenAll(
            Enumerable.Range(0, 10).Select(_ => _client.WithdrawAsync(Checking, amount)));

        responses.Select(r => r.StatusCode).Should().OnlyContain(s =>
            s == HttpStatusCode.OK || s == HttpStatusCode.Conflict || s == HttpStatusCode.UnprocessableEntity);

        var succeeded = responses.Count(r => r.StatusCode == HttpStatusCode.OK);
        succeeded.Should().BeInRange(1, 5);

        var account = await _client.GetAccountAsync(Checking);
        account.Balance.Should().Be(opening - amount * succeeded).And.BeGreaterThanOrEqualTo(0);

        var history = await _client.GetTransactionsAsync(Checking);
        history.Items.Count(t => t.Type == "Withdrawal").Should().Be(succeeded);
        history.Items[0].BalanceAfter.Should().Be(account.Balance);
    }

    [Fact]
    public async Task Parallel_transfers_in_both_directions_preserve_the_total()
    {
        var total = (await _client.GetAccountAsync(Checking)).Balance + (await _client.GetAccountAsync(Savings)).Balance;

        var responses = await Task.WhenAll(Enumerable.Range(0, 10).Select(i =>
            i % 2 == 0
                ? _client.TransferAsync(Checking, Savings, 10m)
                : _client.TransferAsync(Savings, Checking, 10m)));

        responses.Select(r => r.StatusCode).Should().OnlyContain(s =>
            s == HttpStatusCode.OK || s == HttpStatusCode.Conflict);

        var newTotal = (await _client.GetAccountAsync(Checking)).Balance + (await _client.GetAccountAsync(Savings)).Balance;
        newTotal.Should().Be(total, "money is never created or destroyed");
    }

    private static IAccountRepository Repository(IServiceScope scope) =>
        scope.ServiceProvider.GetRequiredService<IAccountRepository>();

    private static IUnitOfWork UnitOfWork(IServiceScope scope) =>
        scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

    private async Task<Dictionary<Guid, long>> VersionsAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AtmDbContext>();
        return await db.Accounts
            .Select(a => new { a.Id, Version = EF.Property<long>(a, AccountConfiguration.VersionProperty) })
            .ToDictionaryAsync(a => a.Id, a => a.Version);
    }
}
