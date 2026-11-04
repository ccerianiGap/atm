using Atm.Domain.Accounts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Atm.Infrastructure.Persistence;

public sealed class DatabaseSeeder(AtmDbContext db, TimeProvider timeProvider, ILogger<DatabaseSeeder> logger)
{
    public static readonly Guid CheckingAccountId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid SavingsAccountId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        // EnsureCreated keeps setup to zero steps; a real deployment would use migrations.
        await db.Database.EnsureCreatedAsync(cancellationToken);

        // WAL lets readers proceed while a write is in progress.
        await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;", cancellationToken);

        if (await db.Accounts.AnyAsync(cancellationToken))
        {
            return;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        db.Accounts.AddRange(
            Account.Open(CheckingAccountId, "1000-0001", "Checking", 1_000m, now),
            Account.Open(SavingsAccountId, "1000-0002", "Savings", 5_000m, now));

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded the database with the default accounts");
    }
}
