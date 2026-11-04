using Atm.Application.Abstractions;
using Atm.Application.Accounts;
using Atm.Application.Configuration;
using Atm.Application.Contracts;
using Atm.Application.Exceptions;
using Atm.Application.Validation;
using Atm.Domain.Accounts;
using Atm.Domain.Exceptions;
using AwesomeAssertions;
using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace Atm.Application.Tests;

public class AccountCommandServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeTimeProvider _time = new(Now);
    private readonly InMemoryAccountRepository _repository = new();
    private readonly CountingUnitOfWork _unitOfWork = new();
    private readonly Account _checking;
    private readonly Account _savings;
    private readonly AccountCommandService _service;

    public AccountCommandServiceTests()
    {
        _checking = Account.Open(Guid.NewGuid(), "1000-0001", "Checking", 100m, Now.UtcDateTime);
        _savings = Account.Open(Guid.NewGuid(), "1000-0002", "Savings", 50m, Now.UtcDateTime);
        _repository.Add(_checking);
        _repository.Add(_savings);

        var options = Options.Create(new AtmOptions());
        _service = new AccountCommandService(
            _repository,
            _unitOfWork,
            new DepositRequestValidator(options),
            new WithdrawRequestValidator(options),
            new TransferRequestValidator(options),
            _time,
            NullLogger<AccountCommandService>.Instance);
    }

    [Fact]
    public async Task Deposit_returns_updated_account_and_commits_once()
    {
        _time.Advance(TimeSpan.FromMinutes(5));

        var result = await _service.DepositAsync(_checking.Id, new DepositRequest(10m), CancellationToken.None);

        result.Account.Balance.Should().Be(110m);
        result.Transaction.Type.Should().Be(nameof(TransactionType.Deposit));
        result.Transaction.CreatedAtUtc.Should().Be(Now.AddMinutes(5).UtcDateTime);
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Withdraw_from_unknown_account_throws_not_found()
    {
        var act = () => _service.WithdrawAsync(Guid.NewGuid(), new WithdrawRequest(20m), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Invalid_request_is_rejected_before_touching_the_account()
    {
        var act = () => _service.WithdrawAsync(_checking.Id, new WithdrawRequest(30m), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        _checking.Balance.Should().Be(100m);
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Withdraw_over_balance_surfaces_the_domain_error_and_does_not_commit()
    {
        var act = () => _service.WithdrawAsync(_checking.Id, new WithdrawRequest(120m), CancellationToken.None);

        await act.Should().ThrowAsync<InsufficientFundsException>();
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Transfer_updates_both_accounts_in_a_single_commit()
    {
        var result = await _service.TransferAsync(
            new TransferRequest(_checking.Id, _savings.Id, 25m), CancellationToken.None);

        result.FromAccount.Balance.Should().Be(75m);
        result.ToAccount.Balance.Should().Be(75m);
        result.Debit.TransferId.Should().Be(result.TransferId);
        result.Credit.TransferId.Should().Be(result.TransferId);
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Transfer_to_unknown_account_throws_not_found_and_does_not_commit()
    {
        var act = () => _service.TransferAsync(
            new TransferRequest(_checking.Id, Guid.NewGuid(), 25m), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _checking.Balance.Should().Be(100m);
        _unitOfWork.SaveCount.Should().Be(0);
    }

    private sealed class InMemoryAccountRepository : IAccountRepository
    {
        private readonly Dictionary<Guid, Account> _accounts = [];

        public void Add(Account account) => _accounts[account.Id] = account;

        public Task<IReadOnlyList<Account>> GetAllAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Account>>(_accounts.Values.ToList());

        public Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(_accounts.GetValueOrDefault(id));

        public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(_accounts.ContainsKey(id));

        public Task<(IReadOnlyList<Transaction> Items, int TotalCount)> GetTransactionsAsync(
            Guid accountId, int page, int pageSize, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class CountingUnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            return Task.CompletedTask;
        }
    }
}
