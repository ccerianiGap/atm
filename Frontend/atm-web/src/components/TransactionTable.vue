<script setup lang="ts">
import type { Transaction, TransactionType } from '@/api/types'
import { formatCurrency, formatDateTime } from '@/utils/format'

defineProps<{ transactions: Transaction[] }>()

const TYPE_LABELS: Record<TransactionType, string> = {
  Deposit: 'Deposit',
  Withdrawal: 'Withdrawal',
  TransferIn: 'Transfer in',
  TransferOut: 'Transfer out',
}

const isCredit = (t: Transaction) => t.type === 'Deposit' || t.type === 'TransferIn'
const signedAmount = (t: Transaction) => `${isCredit(t) ? '+' : '−'}${formatCurrency(t.amount)}`
</script>

<template>
  <div class="table-wrapper">
    <table>
      <thead>
        <tr>
          <th scope="col">Date</th>
          <th scope="col">Description</th>
          <th scope="col">Type</th>
          <th scope="col" class="num">Amount</th>
          <th scope="col" class="num">Balance</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="t in transactions" :key="t.id">
          <td class="date">{{ formatDateTime(t.createdAtUtc) }}</td>
          <td>{{ t.description }}</td>
          <td>
            <span class="badge" :class="isCredit(t) ? 'credit' : 'debit'">{{
              TYPE_LABELS[t.type]
            }}</span>
          </td>
          <td class="num amount" :class="isCredit(t) ? 'credit' : 'debit'">
            {{ signedAmount(t) }}
          </td>
          <td class="num">{{ formatCurrency(t.balanceAfter) }}</td>
        </tr>
      </tbody>
    </table>
  </div>
</template>

<style scoped>
.table-wrapper {
  overflow-x: auto;
}
table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.95rem;
}
th,
td {
  padding: 0.7rem 0.75rem;
  text-align: left;
  border-bottom: 1px solid var(--border);
  white-space: nowrap;
}
th {
  color: var(--muted);
  font-size: 0.8rem;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.04em;
}
.num {
  text-align: right;
  font-variant-numeric: tabular-nums;
}
.date {
  color: var(--muted);
}
.amount.credit {
  color: var(--success);
}
.amount.debit {
  color: var(--danger);
}
.badge {
  display: inline-block;
  padding: 0.15rem 0.5rem;
  border-radius: 999px;
  font-size: 0.8rem;
  font-weight: 600;
}
.badge.credit {
  background: var(--success-bg);
  color: var(--success-text);
}
.badge.debit {
  background: var(--danger-bg);
  color: var(--danger-text);
}
</style>
