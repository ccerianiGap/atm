using Atm.Application.Abstractions;
using Atm.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Atm.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "Atm";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // The connection string is resolved lazily so hosts (e.g. integration tests) can override it.
        services.AddDbContext<AtmDbContext>((provider, options) =>
        {
            var connectionString = provider.GetRequiredService<IConfiguration>().GetConnectionString(ConnectionStringName)
                ?? throw new InvalidOperationException($"Connection string '{ConnectionStringName}' is not configured.");

            // Under concurrent writes, returning a connection to the Microsoft.Data.Sqlite pool intermittently
            // fails ("unable to delete/modify user-function due to active statements"), surfacing as a 500.
            // Opening a SQLite connection is just a local file open, so pooling buys little here.
            var builder = new SqliteConnectionStringBuilder(connectionString) { Pooling = false };
            options.UseSqlite(builder.ConnectionString);
        });

        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<DatabaseSeeder>();

        return services;
    }
}
