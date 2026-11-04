using Atm.Application.Configuration;
using Atm.Application.Contracts;
using Atm.Application.Validation;
using AwesomeAssertions;
using Microsoft.Extensions.Options;

namespace Atm.Application.Tests;

public class ValidatorTests
{
    private static IOptions<AtmOptions> Options(int withdrawalMultiple = 20, decimal max = 10_000m) =>
        Microsoft.Extensions.Options.Options.Create(new AtmOptions
        {
            WithdrawalMultiple = withdrawalMultiple,
            MaxTransactionAmount = max,
        });

    [Theory]
    [InlineData(20, true)]
    [InlineData(60, true)]
    [InlineData(500, true)]
    [InlineData(50, false)]
    [InlineData(20.5, false)]
    [InlineData(0, false)]
    [InlineData(-20, false)]
    public void Withdrawal_must_be_a_positive_multiple_of_20(decimal amount, bool expectedValid)
    {
        var result = new WithdrawRequestValidator(Options()).Validate(new WithdrawRequest(amount));

        result.IsValid.Should().Be(expectedValid);
    }

    [Fact]
    public void Withdrawal_multiple_rule_is_disabled_when_set_to_zero()
    {
        var result = new WithdrawRequestValidator(Options(withdrawalMultiple: 0)).Validate(new WithdrawRequest(55.25m));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Withdrawal_above_the_maximum_is_rejected()
    {
        var result = new WithdrawRequestValidator(Options(max: 1_000m)).Validate(new WithdrawRequest(1_020m));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be(nameof(WithdrawRequest.Amount));
    }

    [Theory]
    [InlineData(0.01, true)]
    [InlineData(10_000, true)]
    [InlineData(10_000.01, false)]
    [InlineData(1.005, false)]
    [InlineData(0, false)]
    public void Deposit_amount_rules(decimal amount, bool expectedValid)
    {
        var result = new DepositRequestValidator(Options()).Validate(new DepositRequest(amount));

        result.IsValid.Should().Be(expectedValid);
    }

    [Fact]
    public void Transfer_does_not_require_a_multiple_of_20()
    {
        var result = new TransferRequestValidator(Options())
            .Validate(new TransferRequest(Guid.NewGuid(), Guid.NewGuid(), 33.33m));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Transfer_to_the_same_account_is_rejected()
    {
        var id = Guid.NewGuid();

        var result = new TransferRequestValidator(Options()).Validate(new TransferRequest(id, id, 10m));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be(nameof(TransferRequest.ToAccountId));
    }

    [Fact]
    public void Transfer_requires_both_account_ids()
    {
        var result = new TransferRequestValidator(Options()).Validate(new TransferRequest(Guid.Empty, Guid.Empty, 10m));

        result.IsValid.Should().BeFalse();
        result.Errors.Select(e => e.PropertyName).Should()
            .Contain([nameof(TransferRequest.FromAccountId), nameof(TransferRequest.ToAccountId)]);
    }
}
