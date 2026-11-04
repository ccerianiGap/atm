// Mirrors the backend contracts (Atm.Application/Contracts).

export interface Account {
  id: string
  accountNumber: string
  name: string
  balance: number
}

export type TransactionType = 'Deposit' | 'Withdrawal' | 'TransferIn' | 'TransferOut'

export interface Transaction {
  id: number
  accountId: string
  type: TransactionType
  amount: number
  balanceAfter: number
  createdAtUtc: string
  description: string
  transferId: string | null
  counterpartyAccountId: string | null
}

export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export interface OperationResult {
  account: Account
  transaction: Transaction
}

export interface TransferResult {
  transferId: string
  fromAccount: Account
  toAccount: Account
  debit: Transaction
  credit: Transaction
}

export interface AtmSettings {
  maxTransactionAmount: number
  withdrawalMultiple: number
}

/** RFC 7807 body returned by the API for every error. */
export interface ProblemDetails {
  type?: string
  title?: string
  status?: number
  detail?: string
  code?: string
  errors?: Record<string, string[]>
}
