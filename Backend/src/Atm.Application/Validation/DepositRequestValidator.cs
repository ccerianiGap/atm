using Atm.Application.Configuration;
using Atm.Application.Contracts;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace Atm.Application.Validation;

public sealed class DepositRequestValidator : AbstractValidator<DepositRequest>
{
    public DepositRequestValidator(IOptions<AtmOptions> options)
    {
        RuleFor(x => x.Amount).ValidAmount(options.Value);
    }
}
