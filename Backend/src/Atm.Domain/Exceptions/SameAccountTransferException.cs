namespace Atm.Domain.Exceptions;

public sealed class SameAccountTransferException(Guid accountId)
    : DomainException("same_account_transfer", "Source and destination accounts must be different.")
{
    public Guid AccountId { get; } = accountId;
}
