namespace Atm.Application.Configuration;

/// <summary>
/// ATM-machine policies (as opposed to account invariants, which live in the domain).
/// </summary>
public sealed class AtmOptions
{
    public const string SectionName = "Atm";

    /// <summary>Upper limit for a single deposit, withdrawal or transfer.</summary>
    public decimal MaxTransactionAmount { get; set; } = 10_000m;

    /// <summary>Withdrawals must be a multiple of this value (note denomination). 0 disables the rule.</summary>
    public int WithdrawalMultiple { get; set; } = 20;

    public bool IsValid() => MaxTransactionAmount > 0 && WithdrawalMultiple >= 0;
}
