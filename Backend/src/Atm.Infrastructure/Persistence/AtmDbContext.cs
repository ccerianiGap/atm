using Atm.Domain.Accounts;
using Atm.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Atm.Infrastructure.Persistence;

public sealed class AtmDbContext(DbContextOptions<AtmDbContext> options) : DbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Transaction> Transactions => Set<Transaction>();

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        BumpAccountVersions();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        BumpAccountVersions();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AtmDbContext).Assembly);

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();

    /// <summary>
    /// SQLite has no rowversion, so the concurrency token is a counter maintained here.
    /// EF sends the original value in the UPDATE's WHERE clause; if another operation
    /// committed first, no row matches and a <see cref="DbUpdateConcurrencyException"/> is thrown.
    /// </summary>
    private void BumpAccountVersions()
    {
        ChangeTracker.DetectChanges();

        foreach (var entry in ChangeTracker.Entries<Account>().Where(e => e.State == EntityState.Modified))
        {
            var version = entry.Property<long>(AccountConfiguration.VersionProperty);
            version.CurrentValue = version.OriginalValue + 1;
        }
    }
}
