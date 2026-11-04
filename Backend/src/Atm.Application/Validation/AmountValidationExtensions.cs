using Atm.Application.Configuration;
using Atm.Domain.Accounts;
using FluentValidation;

namespace Atm.Application.Validation;

internal static class AmountValidationExtensions
{
    /// <summary>Rules shared by every operation amount.</summary>
    public static IRuleBuilderOptions<T, decimal> ValidAmount<T>(this IRuleBuilder<T, decimal> rule, AtmOptions options) =>
        rule
            .GreaterThan(0)
            .WithMessage("Amount must be greater than zero.")
            .Must(amount => decimal.Round(amount, AmountRules.MaxDecimalPlaces) == amount)
            .WithMessage($"Amount must have at most {AmountRules.MaxDecimalPlaces} decimal places.")
            .LessThanOrEqualTo(options.MaxTransactionAmount)
            .WithMessage($"Amount cannot exceed {options.MaxTransactionAmount:0.00} per operation.");
}
