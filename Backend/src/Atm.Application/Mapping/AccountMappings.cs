using Atm.Application.Contracts;
using Atm.Domain.Accounts;

namespace Atm.Application.Mapping;

public static class AccountMappings
{
    public static AccountDto ToDto(this Account account) =>
        new(account.Id, account.AccountNumber, account.Name, account.Balance);
}
