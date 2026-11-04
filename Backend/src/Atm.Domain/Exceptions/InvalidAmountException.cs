namespace Atm.Domain.Exceptions;

public sealed class InvalidAmountException(decimal amount)
    : DomainException("invalid_amount", $"Amount {amount} is invalid. It must be greater than zero and have at most 2 decimal places.")
{
    public decimal Amount { get; } = amount;
}
