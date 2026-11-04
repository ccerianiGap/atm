using System.Net.Http.Json;
using System.Text.Json;
using Atm.Application.Contracts;

namespace Atm.Api.IntegrationTests;

internal static class ApiClientExtensions
{
    public static Task<HttpResponseMessage> DepositAsync(this HttpClient client, Guid accountId, decimal amount) =>
        client.PostAsJsonAsync($"/api/accounts/{accountId}/deposits", new DepositRequest(amount));

    public static Task<HttpResponseMessage> WithdrawAsync(this HttpClient client, Guid accountId, decimal amount) =>
        client.PostAsJsonAsync($"/api/accounts/{accountId}/withdrawals", new WithdrawRequest(amount));

    public static Task<HttpResponseMessage> TransferAsync(this HttpClient client, Guid from, Guid to, decimal amount) =>
        client.PostAsJsonAsync("/api/transfers", new TransferRequest(from, to, amount));

    public static async Task<AccountDto> GetAccountAsync(this HttpClient client, Guid accountId) =>
        (await client.GetFromJsonAsync<AccountDto>($"/api/accounts/{accountId}"))!;

    public static async Task<PagedResult<TransactionDto>> GetTransactionsAsync(
        this HttpClient client, Guid accountId, int page = 1, int pageSize = 100) =>
        (await client.GetFromJsonAsync<PagedResult<TransactionDto>>(
            $"/api/accounts/{accountId}/transactions?page={page}&pageSize={pageSize}"))!;

    public static async Task<JsonElement> ReadProblemAsync(this HttpResponseMessage response)
    {
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }
}
