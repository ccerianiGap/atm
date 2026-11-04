using Atm.Api.Endpoints;
using Atm.Api.ErrorHandling;
using Atm.Application;
using Atm.Application.Configuration;
using Atm.Infrastructure;
using Atm.Infrastructure.Persistence;
using Scalar.AspNetCore;

const string FrontendCorsPolicy = "Frontend";

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOptions<AtmOptions>()
    .Bind(builder.Configuration.GetSection(AtmOptions.SectionName))
    .Validate(o => o.IsValid(), "Atm options are invalid: MaxTransactionAmount must be > 0 and WithdrawalMultiple >= 0.")
    .ValidateOnStart();

builder.Services.AddApplication();
builder.Services.AddInfrastructure();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

// The Vite dev server proxies /api, so CORS is only a fallback for running the client on its own origin.
builder.Services.AddCors(options => options.AddPolicy(FrontendCorsPolicy, policy =>
    policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
        .AllowAnyHeader()
        .AllowAnyMethod()));

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors(FrontendCorsPolicy);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapHealthChecks("/health");
app.MapAccountEndpoints();
app.MapTransferEndpoints();
app.MapSettingsEndpoints();

using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<DatabaseSeeder>().SeedAsync();
}

await app.RunAsync();

// Exposes the entry point to WebApplicationFactory in the integration tests.
public partial class Program;
