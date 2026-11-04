<script setup lang="ts">
import { ref } from 'vue'
import type { Receipt } from '@/utils/receipts'
import type { OperationTab } from './operations'
import DepositForm from './DepositForm.vue'
import WithdrawForm from './WithdrawForm.vue'
import TransferForm from './TransferForm.vue'
import ReceiptDialog from './ReceiptDialog.vue'

const activeTab = defineModel<OperationTab>('tab', { required: true })
const accountId = defineModel<string>('accountId', { required: true })

const tabs: { id: OperationTab; label: string }[] = [
  { id: 'deposit', label: 'Deposit' },
  { id: 'withdraw', label: 'Withdraw' },
  { id: 'transfer', label: 'Transfer' },
]

const receipt = ref<Receipt | null>(null)

/** Arrow-key navigation between tabs (WAI-ARIA tabs pattern). */
function onTabKeydown(event: KeyboardEvent, index: number) {
  const offset = event.key === 'ArrowRight' ? 1 : event.key === 'ArrowLeft' ? -1 : 0
  if (offset === 0) return
  const next = tabs[(index + offset + tabs.length) % tabs.length]!
  activeTab.value = next.id
  document.getElementById(`tab-${next.id}`)?.focus()
}
</script>

<template>
  <section class="panel" aria-label="Operations">
    <div class="tabs" role="tablist">
      <button
        v-for="(tab, index) in tabs"
        :id="`tab-${tab.id}`"
        :key="tab.id"
        type="button"
        role="tab"
        :aria-selected="activeTab === tab.id"
        :aria-controls="`panel-${tab.id}`"
        :tabindex="activeTab === tab.id ? 0 : -1"
        @click="activeTab = tab.id"
        @keydown="onTabKeydown($event, index)"
      >
        {{ tab.label }}
      </button>
    </div>

    <div
      :id="`panel-${activeTab}`"
      class="body"
      role="tabpanel"
      :aria-labelledby="`tab-${activeTab}`"
    >
      <DepositForm
        v-if="activeTab === 'deposit'"
        v-model:account-id="accountId"
        @completed="receipt = $event"
      />
      <WithdrawForm
        v-else-if="activeTab === 'withdraw'"
        v-model:account-id="accountId"
        @completed="receipt = $event"
      />
      <TransferForm v-else v-model:account-id="accountId" @completed="receipt = $event" />
    </div>

    <ReceiptDialog v-if="receipt" :receipt="receipt" @close="receipt = null" />
  </section>
</template>

<style scoped>
.panel {
  border: 1px solid var(--border);
  border-radius: var(--radius-lg);
  background: var(--surface);
  overflow: hidden;
}
.tabs {
  display: flex;
  border-bottom: 1px solid var(--border);
  background: var(--surface-2);
}
[role='tab'] {
  flex: 1;
  padding: 0.9rem 0.5rem;
  border: none;
  border-bottom: 3px solid transparent;
  background: none;
  color: var(--muted);
  font-weight: 600;
  cursor: pointer;
}
[role='tab'][aria-selected='true'] {
  color: var(--primary);
  border-bottom-color: var(--primary);
  background: var(--surface);
}
.body {
  padding: 1.25rem;
}
</style>
