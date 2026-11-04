using Atm.Domain.Accounts;
using Atm.Domain.Exceptions;
using AwesomeAssertions;

namespace Atm.Domain.Tests;

public class AccountTests
{
    private static readonly DateTime Now = new(2026, 10, 30, 12, 0, 0, DateTimeKind.Utc);

    private static Account NewAccount(decimal openingBalance = 100m) =>
        Account.Open(Guid.NewGuid(), "1000-0001", "Checking", openingBalance, Now);

    [Fact]
    public void Open_with_balance_records_an_opening_deposit()
    {
        var account = NewAccount(250m);

        account.Balance.Should().Be(250m);
        account.Transactions.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { Type = TransactionType.Deposit, Amount = 250m, BalanceAfter = 250m });
    }

    [Fact]
    public void Open_with_zero_balance_records_no_transaction()
    {
        var account = NewAccount(0m);

        account.Balance.Should().Be(0m);
        account.Transactions.Should().BeEmpty();
    }

    [Fact]
    public void Deposit_increases_balance_and_records_transaction()
    {
        var account = NewAccount(100m);

        var transaction = account.Deposit(25.50m, Now);

        account.Balance.Should().Be(125.50m);
        transaction.Type.Should().Be(TransactionType.Deposit);
        transaction.Amount.Should().Be(25.50m);
        transaction.BalanceAfter.Should().Be(125.50m);
        transaction.AccountId.Should().Be(account.Id);
        transaction.CreatedAtUtc.Should().Be(Now);
    }

    [Fact]
    public void Withdraw_decreases_balance_and_records_transaction()
    {
        var account = NewAccount(100m);

        var transaction = account.Withdraw(40m, Now);

        account.Balance.Should().Be(60m);
        transaction.Type.Should().Be(TransactionType.Withdrawal);
        transaction.BalanceAfter.Should().Be(60m);
    }

    [Fact]
    public void Withdraw_entire_balance_is_allowed()
    {
        var account = NewAccount(100m);

        account.Withdraw(100m, Now);

        account.Balance.Should().Be(0m);
    }

    [Fact]
    public void Withdraw_more_than_balance_throws_and_leaves_state_unchanged()
    {
        var account = NewAccount(100m);
        var transactionsBefore = account.Transactions.Count;

        var act = () => account.Withdraw(100.01m, Now);

        act.Should().Throw<InsufficientFundsException>()
            .Which.Code.Should().Be("insufficient_funds");
        account.Balance.Should().Be(100m);
        account.Transactions.Should().HaveCount(transactionsBefore);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10.001)]
    public void Deposit_with_invalid_amount_throws(decimal amount)
    {
        var account = NewAccount();

        var act = () => account.Deposit(amount, Now);

        act.Should().Throw<InvalidAmountException>();
        account.Balance.Should().Be(100m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-20)]
    [InlineData(0.005)]
    public void Withdraw_with_invalid_amount_throws(decimal amount)
    {
        var account = NewAccount();

        var act = () => account.Withdraw(amount, Now);

        act.Should().Throw<InvalidAmountException>();
    }
}
