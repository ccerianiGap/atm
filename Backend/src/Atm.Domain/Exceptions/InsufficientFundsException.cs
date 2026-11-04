using System.Globalization;

namespace Atm.Domain.Exceptions;

public sealed class InsufficientFundsException(Guid accountId, decimal requested, decimal available)
    : DomainException(
        "insufficient_funds",
        string.Format(CultureInfo.InvariantCulture, "Insufficient funds: requested {0:0.00}, available {1:0.00}.", requested, available))
{
    public Guid AccountId { get; } = accountId;
    public decimal Requested { get; } = requested;
    public decimal Available { get; } = available;
}
