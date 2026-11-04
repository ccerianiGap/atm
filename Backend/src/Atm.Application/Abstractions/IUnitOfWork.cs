namespace Atm.Application.Abstractions;

public interface IUnitOfWork
{
    /// <summary>
    /// Persists all pending changes atomically.
    /// </summary>
    /// <exception cref="Exceptions.ConcurrencyConflictException">
    /// Another operation modified one of the affected accounts since it was loaded.
    /// </exception>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
