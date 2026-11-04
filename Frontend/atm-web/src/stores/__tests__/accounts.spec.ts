import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { accountsApi } from '@/api/accountsApi'
import { ApiError } from '@/api/httpClient'
import type { Account, OperationResult } from '@/api/types'
import { useAccountsStore } from '../accounts'

type AccountsApi = typeof accountsApi

vi.mock('@/api/accountsApi', () => ({
  accountsApi: {
    getAccounts: vi.fn<AccountsApi['getAccounts']>(),
    withdraw: vi.fn<AccountsApi['withdraw']>(),
  },
}))

const checking: Account = { id: 'c', accountNumber: '1000-0001', name: 'Checking', balance: 100 }
const savings: Account = { id: 's', accountNumber: '1000-0002', name: 'Savings', balance: 50 }

describe('accounts store', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.resetAllMocks()
  })

  it('applies the balance returned by a withdrawal', async () => {
    const store = useAccountsStore()
    store.accounts = [checking, savings]
    vi.mocked(accountsApi.withdraw).mockResolvedValue({
      account: { ...checking, balance: 80 },
      transaction: { id: 1 },
    } as OperationResult)

    await store.withdraw('c', 20)

    expect(store.accountById('c')?.balance).toBe(80)
    expect(store.accountById('s')?.balance).toBe(50)
  })

  it('refreshes balances and rethrows on a concurrency conflict', async () => {
    const store = useAccountsStore()
    store.accounts = [checking]
    vi.mocked(accountsApi.withdraw).mockRejectedValue(new ApiError('Conflict', 409))
    vi.mocked(accountsApi.getAccounts).mockResolvedValue([{ ...checking, balance: 60 }])

    await expect(store.withdraw('c', 20)).rejects.toBeInstanceOf(ApiError)

    expect(accountsApi.getAccounts).toHaveBeenCalledOnce()
    expect(store.accountById('c')?.balance).toBe(60)
  })

  it('lists every other account as a transfer target', () => {
    const store = useAccountsStore()
    store.accounts = [checking, savings]

    expect(store.transferTargets('c').map((a) => a.id)).toEqual(['s'])
  })
})
