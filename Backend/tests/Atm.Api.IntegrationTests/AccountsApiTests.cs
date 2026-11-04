using System.Net;
using System.Net.Http.Json;
using Atm.Application.Contracts;
using Atm.Infrastructure.Persistence;

namespace Atm.Api.IntegrationTests;

public sealed class AccountsApiTests : IDisposable
{
    private static readonly Guid Checking = DatabaseSeeder.CheckingAccountId;
    private static readonly Guid Savings = DatabaseSeeder.SavingsAccountId;

    private readonly AtmApiFactory _factory = new();
    private readonly HttpClient _client;

    public AccountsApiTests() => _client = _factory.CreateClient();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Get_accounts_returns_the_seeded_accounts()
    {
        var accounts = await _client.GetFromJsonAsync<List<AccountDto>>("/api/accounts");

        accounts.Should().BeEquivalentTo(
        [
            new AccountDto(Checking, "1000-0001", "Checking", 1_000m),
            new AccountDto(Savings, "1000-0002", "Savings", 5_000m),
        ]);
    }

    [Fact]
    public async Task Deposit_updates_balance_and_history()
    {
        var response = await _client.DepositAsync(Checking, 12.34m);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<OperationResultDto>();
        result!.Account.Balance.Should().Be(1_012.34m);
        result.Transaction.BalanceAfter.Should().Be(1_012.34m);

        var history = await _client.GetTransactionsAsync(Checking);
        history.Items[0].Should().BeEquivalentTo(result.Transaction, "the newest transaction comes first");
    }

    [Fact]
    public async Task Timestamps_are_serialized_as_utc()
    {
        var response = await _client.DepositAsync(Checking, 1m);
        var json = await response.Content.ReadAsStringAsync();

        json.Should().MatchRegex("\"createdAtUtc\":\"[^\"]+Z\"");
    }

    [Fact]
    public async Task Withdrawal_over_balance_returns_422_problem_with_code()
    {
        var response = await _client.WithdrawAsync(Checking, 1_020m);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var problem = await response.ReadProblemAsync();
        problem.GetProperty("code").GetString().Should().Be("insufficient_funds");
        (await _client.GetAccountAsync(Checking)).Balance.Should().Be(1_000m);
    }

    [Fact]
    public async Task Withdrawal_not_multiple_of_20_returns_400_with_field_error()
    {
        var response = await _client.WithdrawAsync(Checking, 50m);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.ReadProblemAsync();
        problem.GetProperty("code").GetString().Should().Be("validation_failed");
        problem.GetProperty("errors").GetProperty("amount")[0].GetString().Should().Contain("multiples of 20");
    }

    [Fact]
    public async Task Malformed_body_returns_400()
    {
        var response = await _client.PostAsync(
            $"/api/accounts/{Checking}/deposits",
            new StringContent("{\"amount\":\"abc\"}", System.Text.Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Unknown_account_returns_404()
    {
        var response = await _client.DepositAsync(Guid.NewGuid(), 10m);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.ReadProblemAsync()).GetProperty("code").GetString().Should().Be("not_found");
    }

    [Fact]
    public async Task Transfer_updates_both_accounts_and_both_histories()
    {
        var response = await _client.TransferAsync(Savings, Checking, 250.50m);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TransferResultDto>();
        result!.FromAccount.Balance.Should().Be(4_749.50m);
        result.ToAccount.Balance.Should().Be(1_250.50m);

        (await _client.GetTransactionsAsync(Savings)).Items[0].TransferId.Should().Be(result.TransferId);
        (await _client.GetTransactionsAsync(Checking)).Items[0].TransferId.Should().Be(result.TransferId);
    }

    [Fact]
    public async Task Transfer_to_same_account_returns_400()
    {
        var response = await _client.TransferAsync(Checking, Checking, 10m);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Transaction_history_is_paged_newest_first()
    {
        for (var i = 1; i <= 5; i++)
        {
            await _client.DepositAsync(Checking, i);
        }

        var page1 = await _client.GetTransactionsAsync(Checking, page: 1, pageSize: 2);
        var page2 = await _client.GetTransactionsAsync(Checking, page: 2, pageSize: 2);

        page1.TotalCount.Should().Be(6, "5 deposits plus the opening deposit");
        page1.TotalPages.Should().Be(3);
        page1.Items.Select(t => t.Amount).Should().Equal(5m, 4m);
        page2.Items.Select(t => t.Amount).Should().Equal(3m, 2m);
    }

    [Fact]
    public async Task Invalid_paging_returns_400()
    {
        var response = await _client.GetAsync($"/api/accounts/{Checking}/transactions?page=0&pageSize=500");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var errors = (await response.ReadProblemAsync()).GetProperty("errors");
        errors.TryGetProperty("page", out _).Should().BeTrue();
        errors.TryGetProperty("pageSize", out _).Should().BeTrue();
    }
}
