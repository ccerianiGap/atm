using Atm.Application.Configuration;
using Atm.Application.Contracts;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace Atm.Application.Validation;

public sealed class WithdrawRequestValidator : AbstractValidator<WithdrawRequest>
{
    public WithdrawRequestValidator(IOptions<AtmOptions> options)
    {
        var settings = options.Value;

        RuleFor(x => x.Amount).ValidAmount(settings);

        // ATM policy: the machine dispenses notes of a single denomination.
        RuleFor(x => x.Amount)
            .Must(amount => amount % settings.WithdrawalMultiple == 0)
            .When(_ => settings.WithdrawalMultiple > 0)
            .WithMessage($"Withdrawals must be in multiples of {settings.WithdrawalMultiple}.");
    }
}
