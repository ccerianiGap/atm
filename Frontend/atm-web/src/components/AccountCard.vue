<script setup lang="ts">
import type { Account } from '@/api/types'
import type { OperationTab } from '@/components/operations'
import { formatCurrency, maskAccountNumber } from '@/utils/format'

defineProps<{
  account: Account
  selected: boolean
}>()

const emit = defineEmits<{
  select: []
  action: [tab: OperationTab]
}>()

const shortcuts: { tab: OperationTab; label: string }[] = [
  { tab: 'deposit', label: 'Deposit' },
  { tab: 'withdraw', label: 'Withdraw' },
  { tab: 'transfer', label: 'Transfer' },
]
</script>

<template>
  <article class="card" :class="{ selected }">
    <button type="button" class="summary" :aria-pressed="selected" @click="emit('select')">
      <span class="name">{{ account.name }}</span>
      <span class="number">{{ maskAccountNumber(account.accountNumber) }}</span>
      <span class="balance-label">Available balance</span>
      <span class="balance">{{ formatCurrency(account.balance) }}</span>
    </button>
    <div class="actions">
      <button
        v-for="shortcut in shortcuts"
        :key="shortcut.tab"
        type="button"
        class="link-button"
        @click="emit('action', shortcut.tab)"
      >
        {{ shortcut.label }}
      </button>
      <RouterLink class="history" :to="{ name: 'account-history', params: { id: account.id } }">
        History →
      </RouterLink>
    </div>
  </article>
</template>

<style scoped>
.card {
  border: 2px solid var(--border);
  border-radius: var(--radius-lg);
  background: var(--surface);
  overflow: hidden;
  transition:
    border-color 0.15s,
    box-shadow 0.15s;
}
.card.selected {
  border-color: var(--primary);
  box-shadow: 0 0 0 3px var(--primary-ring);
}
.summary {
  display: grid;
  width: 100%;
  text-align: left;
  gap: 0.15rem;
  padding: 1.25rem;
  background: none;
  border: none;
  color: inherit;
  cursor: pointer;
}
.name {
  font-size: 1.1rem;
  font-weight: 600;
}
.number {
  color: var(--muted);
  font-size: 0.9rem;
}
.balance-label {
  margin-top: 0.75rem;
  color: var(--muted);
  font-size: 0.8rem;
  text-transform: uppercase;
  letter-spacing: 0.04em;
}
.balance {
  font-size: 1.75rem;
  font-weight: 700;
  font-variant-numeric: tabular-nums;
}
.actions {
  display: flex;
  flex-wrap: wrap;
  gap: 0.25rem 1rem;
  padding: 0.75rem 1.25rem;
  border-top: 1px solid var(--border);
  background: var(--surface-2);
}
.history {
  margin-left: auto;
}
</style>
