<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { storeToRefs } from 'pinia'
import { useAccountsStore } from '@/stores/accounts'
import { useOperationForm } from '@/composables/useOperationForm'
import { validateAmount } from '@/utils/amountValidation'
import { formatCurrency } from '@/utils/format'
import { withdrawalReceipt, type Receipt } from '@/utils/receipts'
import AccountSelect from './AccountSelect.vue'
import AmountInput from './AmountInput.vue'
import AlertBanner from './AlertBanner.vue'

const accountId = defineModel<string>('accountId', { required: true })
const emit = defineEmits<{ completed: [receipt: Receipt] }>()

const store = useAccountsStore()
const { accounts, settings } = storeToRefs(store)

const available = computed(() => store.accountById(accountId.value)?.balance ?? 0)
const multiple = computed(() => settings.value.withdrawalMultiple)

// Quick amounts are multiples of the note denomination, so they always satisfy the server rule.
const quickAmounts = computed(() => [1, 2, 3, 5, 10, 25].map((n) => n * (multiple.value || 20)))

const amount = ref<number | null>(null)
const isOtherAmount = ref(false)

const clientError = computed(() =>
  validateAmount(amount.value, {
    max: settings.value.maxTransactionAmount,
    available: available.value,
    multiple: multiple.value,
  }),
)

const isQuickAmountDisabled = (value: number) =>
  value > available.value || value > settings.value.maxTransactionAmount

function selectQuickAmount(value: number) {
  isOtherAmount.value = false
  amount.value = value
}

function selectOtherAmount() {
  isOtherAmount.value = true
  amount.value = null
}

const { pending, amountError, bannerError, dismissError, onSubmit } = useOperationForm({
  amount,
  clientError,
  submit: async () => withdrawalReceipt(await store.withdraw(accountId.value, amount.value!)),
  onCompleted: (receipt) => {
    isOtherAmount.value = false
    emit('completed', receipt)
  },
})

// A quick amount picked for one account may not be valid for another.
watch(accountId, () => {
  if (!isOtherAmount.value) {
    amount.value = null
  }
})
</script>

<template>
  <form class="operation-form" novalidate @submit.prevent="onSubmit">
    <AccountSelect
      id="withdraw-account"
      v-model="accountId"
      label="Withdraw from"
      :accounts="accounts"
      :disabled="pending"
    />
    <p class="available">
      Available: <strong>{{ formatCurrency(available) }}</strong>
    </p>

    <fieldset class="quick-amounts" :disabled="pending">
      <legend>Amount</legend>
      <button
        v-for="value in quickAmounts"
        :key="value"
        type="button"
        class="tile"
        :class="{ active: !isOtherAmount && amount === value }"
        :aria-pressed="!isOtherAmount && amount === value"
        :disabled="isQuickAmountDisabled(value)"
        @click="selectQuickAmount(value)"
      >
        {{ formatCurrency(value) }}
      </button>
      <button
        type="button"
        class="tile"
        :class="{ active: isOtherAmount }"
        :aria-pressed="isOtherAmount"
        data-testid="other-amount"
        @click="selectOtherAmount"
      >
        Other amount
      </button>
    </fieldset>

    <AmountInput
      v-if="isOtherAmount"
      id="withdraw-amount"
      v-model="amount"
      label="Other amount"
      :allow-decimals="multiple === 0"
      :hint="multiple > 0 ? `Amounts in multiples of ${formatCurrency(multiple)}` : undefined"
      :error="amountError"
      :disabled="pending"
    />
    <p v-else-if="amountError" class="field-error" role="alert">{{ amountError }}</p>

    <AlertBanner v-if="bannerError" kind="error" :message="bannerError" @dismiss="dismissError" />
    <button type="submit" class="primary" :disabled="pending">
      {{ pending ? 'Processing…' : amount ? `Withdraw ${formatCurrency(amount)}` : 'Withdraw' }}
    </button>
  </form>
</template>

<style scoped>
.available {
  margin: -0.5rem 0 0;
  color: var(--muted);
}
.quick-amounts {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(7rem, 1fr));
  gap: 0.5rem;
  border: none;
  padding: 0;
  margin: 0;
}
.quick-amounts legend {
  font-weight: 600;
  margin-bottom: 0.4rem;
  padding: 0;
}
.tile {
  padding: 0.75rem 0.5rem;
  border: 1px solid var(--border);
  border-radius: var(--radius);
  background: var(--surface);
  color: inherit;
  font-weight: 600;
  font-variant-numeric: tabular-nums;
  cursor: pointer;
}
.tile:hover:not(:disabled) {
  border-color: var(--primary);
}
.tile.active {
  background: var(--primary);
  border-color: var(--primary);
  color: var(--on-primary);
}
.tile:disabled {
  opacity: 0.4;
  cursor: not-allowed;
}
</style>
