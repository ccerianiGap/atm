using Atm.Domain.Accounts;
using Atm.Domain.Exceptions;
using AwesomeAssertions;

namespace Atm.Domain.Tests;

public class TransferServiceTests
{
    private static readonly DateTime Now = new(2026, 10, 30, 12, 0, 0, DateTimeKind.Utc);

    private readonly Account _checking = Account.Open(Guid.NewGuid(), "1000-0001", "Checking", 100m, Now);
    private readonly Account _savings = Account.Open(Guid.NewGuid(), "1000-0002", "Savings", 50m, Now);

    [Fact]
    public void Transfer_moves_money_and_preserves_the_total()
    {
        TransferService.Transfer(_checking, _savings, 30m, Now);

        _checking.Balance.Should().Be(70m);
        _savings.Balance.Should().Be(80m);
        (_checking.Balance + _savings.Balance).Should().Be(150m);
    }

    [Fact]
    public void Transfer_creates_two_linked_legs()
    {
        var legs = TransferService.Transfer(_checking, _savings, 30m, Now);

        legs.Debit.Should().BeEquivalentTo(new
        {
            AccountId = _checking.Id,
            Type = TransactionType.TransferOut,
            Amount = 30m,
            BalanceAfter = 70m,
            TransferId = (Guid?)legs.TransferId,
            CounterpartyAccountId = (Guid?)_savings.Id,
            Description = "Transfer to Savings",
        });
        legs.Credit.Should().BeEquivalentTo(new
        {
            AccountId = _savings.Id,
            Type = TransactionType.TransferIn,
            Amount = 30m,
            BalanceAfter = 80m,
            TransferId = (Guid?)legs.TransferId,
            CounterpartyAccountId = (Guid?)_checking.Id,
            Description = "Transfer from Checking",
        });
    }

    [Fact]
    public void Transfer_with_insufficient_funds_changes_neither_account()
    {
        var act = () => TransferService.Transfer(_savings, _checking, 50.01m, Now);

        act.Should().Throw<InsufficientFundsException>();
        _savings.Balance.Should().Be(50m);
        _checking.Balance.Should().Be(100m);
        _checking.Transactions.Should().HaveCount(1, "only the opening deposit exists");
    }

    [Fact]
    public void Transfer_to_the_same_account_throws()
    {
        var act = () => TransferService.Transfer(_checking, _checking, 10m, Now);

        act.Should().Throw<SameAccountTransferException>();
        _checking.Balance.Should().Be(100m);
    }

    [Fact]
    public void Transfer_with_invalid_amount_throws()
    {
        var act = () => TransferService.Transfer(_checking, _savings, -5m, Now);

        act.Should().Throw<InvalidAmountException>();
    }
}
