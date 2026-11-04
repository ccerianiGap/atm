using Atm.Domain.Exceptions;

namespace Atm.Domain.Accounts;

/// <summary>
/// Invariants every monetary amount must satisfy, regardless of the operation.
/// </summary>
public static class AmountRules
{
    public const int MaxDecimalPlaces = 2;

    public static bool IsValid(decimal amount) =>
        amount > 0 && decimal.Round(amount, MaxDecimalPlaces) == amount;

    public static void EnsureValid(decimal amount)
    {
        if (!IsValid(amount))
        {
            throw new InvalidAmountException(amount);
        }
    }
}
