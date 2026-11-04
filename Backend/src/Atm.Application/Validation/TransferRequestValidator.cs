using Atm.Application.Configuration;
using Atm.Application.Contracts;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace Atm.Application.Validation;

public sealed class TransferRequestValidator : AbstractValidator<TransferRequest>
{
    public TransferRequestValidator(IOptions<AtmOptions> options)
    {
        RuleFor(x => x.FromAccountId).NotEmpty();
        RuleFor(x => x.ToAccountId)
            .NotEmpty()
            .NotEqual(x => x.FromAccountId)
            .WithMessage("Source and destination accounts must be different.");
        RuleFor(x => x.Amount).ValidAmount(options.Value);
    }
}
