import { request } from './httpClient'
import type {
  Account,
  AtmSettings,
  OperationResult,
  PagedResult,
  Transaction,
  TransferResult,
} from './types'

// The only module that knows the API's URLs.
export const accountsApi = {
  getAccounts: () => request<Account[]>('/api/accounts'),

  getAccount: (id: string) => request<Account>(`/api/accounts/${id}`),

  getTransactions: (id: string, page: number, pageSize: number) =>
    request<PagedResult<Transaction>>(
      `/api/accounts/${id}/transactions?page=${page}&pageSize=${pageSize}`,
    ),

  deposit: (id: string, amount: number) =>
    request<OperationResult>(`/api/accounts/${id}/deposits`, {
      method: 'POST',
      body: JSON.stringify({ amount }),
    }),

  withdraw: (id: string, amount: number) =>
    request<OperationResult>(`/api/accounts/${id}/withdrawals`, {
      method: 'POST',
      body: JSON.stringify({ amount }),
    }),

  transfer: (fromAccountId: string, toAccountId: string, amount: number) =>
    request<TransferResult>('/api/transfers', {
      method: 'POST',
      body: JSON.stringify({ fromAccountId, toAccountId, amount }),
    }),

  getSettings: () => request<AtmSettings>('/api/atm/settings'),
}
