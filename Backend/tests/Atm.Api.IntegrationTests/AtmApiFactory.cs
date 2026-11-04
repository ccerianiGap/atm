using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Atm.Api.IntegrationTests;

/// <summary>
/// Hosts the real API against its own temporary SQLite file. A file (not a shared in-memory
/// connection) is used because each request gets its own connection, which is what makes
/// the concurrency tests meaningful.
/// </summary>
public sealed class AtmApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"atm-tests-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Atm", $"Data Source={_databasePath}");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        // Connection pooling is disabled by the infrastructure, so no handles remain open here.
        foreach (var suffix in new[] { "", "-wal", "-shm" })
        {
            File.Delete(_databasePath + suffix);
        }
    }
}
