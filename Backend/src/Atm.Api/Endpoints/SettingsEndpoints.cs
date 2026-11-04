using Atm.Application.Configuration;
using Atm.Application.Contracts;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;

namespace Atm.Api.Endpoints;

internal static class SettingsEndpoints
{
    public static IEndpointRouteBuilder MapSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        // Lets the client mirror server-side limits instead of hard-coding them.
        app.MapGet("/api/atm/settings", GetSettings).WithTags("Settings").WithName("GetAtmSettings");
        return app;
    }

    private static Ok<AtmSettingsDto> GetSettings(IOptions<AtmOptions> options) =>
        TypedResults.Ok(new AtmSettingsDto(options.Value.MaxTransactionAmount, options.Value.WithdrawalMultiple));
}
