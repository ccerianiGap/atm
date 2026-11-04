import type { Account, OperationResult, Transaction, TransferResult } from '@/api/types'
import { formatCurrency, maskAccountNumber } from './format'

export interface Receipt {
  title: string
  amount: number
  reference: string
  createdAtUtc: string
  lines: { label: string; value: string }[]
}

const accountLabel = (account: Account) =>
  `${account.name} (${maskAccountNumber(account.accountNumber)})`

const reference = (transaction: Transaction) => `#${transaction.id}`

export function depositReceipt(result: OperationResult): Receipt {
  return {
    title: 'Deposit successful',
    amount: result.transaction.amount,
    reference: reference(result.transaction),
    createdAtUtc: result.transaction.createdAtUtc,
    lines: [
      { label: 'Account', value: accountLabel(result.account) },
      { label: 'New balance', value: formatCurrency(result.account.balance) },
    ],
  }
}

export function withdrawalReceipt(result: OperationResult): Receipt {
  return {
    ...depositReceipt(result),
    title: 'Please take your cash',
  }
}

export function transferReceipt(result: TransferResult): Receipt {
  return {
    title: 'Transfer successful',
    amount: result.debit.amount,
    reference: reference(result.debit),
    createdAtUtc: result.debit.createdAtUtc,
    lines: [
      { label: 'From', value: accountLabel(result.fromAccount) },
      { label: 'To', value: accountLabel(result.toAccount) },
      {
        label: `${result.fromAccount.name} balance`,
        value: formatCurrency(result.fromAccount.balance),
      },
      {
        label: `${result.toAccount.name} balance`,
        value: formatCurrency(result.toAccount.balance),
      },
    ],
  }
}
