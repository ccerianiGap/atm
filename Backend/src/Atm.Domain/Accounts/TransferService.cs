using Atm.Domain.Exceptions;

namespace Atm.Domain.Accounts;

/// <summary>
/// Domain service for the one operation that spans two aggregates.
/// </summary>
public static class TransferService
{
    public static TransferLegs Transfer(Account source, Account destination, decimal amount, DateTime utcNow)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);

        if (source.Id == destination.Id)
        {
            throw new SameAccountTransferException(source.Id);
        }

        var transferId = Guid.NewGuid();

        // Debit first: if it fails (e.g. insufficient funds) nothing has been credited.
        var debit = source.TransferOut(amount, destination, transferId, utcNow);
        var credit = destination.TransferIn(amount, source, transferId, utcNow);

        return new TransferLegs(transferId, debit, credit);
    }
}

public sealed record TransferLegs(Guid TransferId, Transaction Debit, Transaction Credit);
