import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import { accountsApi } from '@/api/accountsApi'
import { ApiError } from '@/api/httpClient'
import type { Account, AtmSettings } from '@/api/types'

/** Used until /api/atm/settings responds (or if it can't be reached). Matches the server defaults. */
export const DEFAULT_SETTINGS: AtmSettings = {
  maxTransactionAmount: 10_000,
  withdrawalMultiple: 20,
}

export const useAccountsStore = defineStore('accounts', () => {
  const accounts = ref<Account[]>([])
  const settings = ref<AtmSettings>({ ...DEFAULT_SETTINGS })
  const loading = ref(false)
  const error = ref<ApiError | null>(null)

  const accountById = computed(() => (id: string) => accounts.value.find((a) => a.id === id))

  /** Every account except the source; works for any number of accounts. */
  const transferTargets = computed(
    () => (fromId: string) => accounts.value.filter((a) => a.id !== fromId),
  )

  async function fetchAccounts() {
    loading.value = true
    error.value = null
    try {
      accounts.value = await accountsApi.getAccounts()
    } catch (e) {
      error.value = ApiError.from(e)
    } finally {
      loading.value = false
    }
  }

  async function fetchSettings() {
    try {
      settings.value = await accountsApi.getSettings()
    } catch {
      // Keep the defaults; the server still enforces the real limits.
    }
  }

  // Operations apply the balances returned by the API (no extra round trip) and return the
  // result so the caller can show a receipt. Errors propagate to the caller.

  async function deposit(accountId: string, amount: number) {
    const result = await withConflictRefresh(() => accountsApi.deposit(accountId, amount))
    applyBalances(result.account)
    return result
  }

  async function withdraw(accountId: string, amount: number) {
    const result = await withConflictRefresh(() => accountsApi.withdraw(accountId, amount))
    applyBalances(result.account)
    return result
  }

  async function transfer(fromAccountId: string, toAccountId: string, amount: number) {
    const result = await withConflictRefresh(() =>
      accountsApi.transfer(fromAccountId, toAccountId, amount),
    )
    applyBalances(result.fromAccount, result.toAccount)
    return result
  }

  /** On a concurrency conflict our balances are stale: refresh them before reporting the error. */
  async function withConflictRefresh<T>(operation: () => Promise<T>): Promise<T> {
    try {
      return await operation()
    } catch (e) {
      if (e instanceof ApiError && e.isConflict) {
        await fetchAccounts()
      }
      throw e
    }
  }

  function applyBalances(...updated: Account[]) {
    for (const account of updated) {
      const index = accounts.value.findIndex((a) => a.id === account.id)
      if (index >= 0) {
        accounts.value[index] = account
      }
    }
  }

  return {
    accounts,
    settings,
    loading,
    error,
    accountById,
    transferTargets,
    fetchAccounts,
    fetchSettings,
    deposit,
    withdraw,
    transfer,
  }
})
