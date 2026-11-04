using Atm.Application.Abstractions;
using Atm.Application.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Atm.Infrastructure.Persistence;

internal sealed class UnitOfWork(AtmDbContext db) : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            // A single SaveChanges runs in one database transaction: all changes commit or none do.
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyConflictException(ex);
        }
    }
}
