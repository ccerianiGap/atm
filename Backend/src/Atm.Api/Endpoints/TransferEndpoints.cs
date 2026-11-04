using Atm.Application.Accounts;
using Atm.Application.Contracts;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Atm.Api.Endpoints;

internal static class TransferEndpoints
{
    public static IEndpointRouteBuilder MapTransferEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/transfers", Transfer).WithTags("Transfers").WithName("Transfer");
        return app;
    }

    private static async Task<Ok<TransferResultDto>> Transfer(
        TransferRequest request, AccountCommandService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.TransferAsync(request, cancellationToken));
}
