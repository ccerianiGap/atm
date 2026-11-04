<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { storeToRefs } from 'pinia'
import { useAccountsStore } from '@/stores/accounts'
import { useOperationForm } from '@/composables/useOperationForm'
import { validateAmount } from '@/utils/amountValidation'
import { formatCurrency } from '@/utils/format'
import { transferReceipt, type Receipt } from '@/utils/receipts'
import AccountSelect from './AccountSelect.vue'
import AmountInput from './AmountInput.vue'
import AlertBanner from './AlertBanner.vue'

/** The source account; bound to the dashboard's selected account. */
const fromAccountId = defineModel<string>('accountId', { required: true })
const emit = defineEmits<{ completed: [receipt: Receipt] }>()

const store = useAccountsStore()
const { accounts, settings } = storeToRefs(store)

const targets = computed(() => store.transferTargets(fromAccountId.value))
const toAccountId = ref('')

// Keep the destination valid whenever the source (or the account list) changes.
watch(
  targets,
  (options) => {
    if (!options.some((a) => a.id === toAccountId.value)) {
      toAccountId.value = options[0]?.id ?? ''
    }
  },
  { immediate: true },
)

const available = computed(() => store.accountById(fromAccountId.value)?.balance ?? 0)
const amount = ref<number | null>(null)
const clientError = computed(() =>
  validateAmount(amount.value, {
    max: settings.value.maxTransactionAmount,
    available: available.value,
  }),
)

const { pending, amountError, bannerError, dismissError, onSubmit } = useOperationForm({
  amount,
  clientError,
  submit: async () =>
    transferReceipt(await store.transfer(fromAccountId.value, toAccountId.value, amount.value!)),
  onCompleted: (receipt) => emit('completed', receipt),
})
</script>

<template>
  <form class="operation-form" novalidate @submit.prevent="onSubmit">
    <div class="accounts">
      <AccountSelect
        id="transfer-from"
        v-model="fromAccountId"
        label="From"
        :accounts="accounts"
        :disabled="pending"
      />
      <span class="arrow" aria-hidden="true">→</span>
      <AccountSelect
        id="transfer-to"
        v-model="toAccountId"
        label="To"
        :accounts="targets"
        :disabled="pending || targets.length === 0"
      />
    </div>
    <p class="available">
      Available: <strong>{{ formatCurrency(available) }}</strong>
    </p>
    <AmountInput
      id="transfer-amount"
      v-model="amount"
      label="Amount"
      :error="amountError"
      :disabled="pending"
    />
    <AlertBanner
      v-if="targets.length === 0"
      kind="info"
      message="There is no other account to transfer to."
    />
    <AlertBanner v-if="bannerError" kind="error" :message="bannerError" @dismiss="dismissError" />
    <button type="submit" class="primary" :disabled="pending || !toAccountId">
      {{ pending ? 'Processing…' : 'Transfer' }}
    </button>
  </form>
</template>

<style scoped>
.accounts {
  display: grid;
  grid-template-columns: 1fr auto 1fr;
  align-items: end;
  gap: 0.75rem;
}
.arrow {
  padding-bottom: 0.6rem;
  color: var(--muted);
  font-size: 1.25rem;
}
.available {
  margin: -0.5rem 0 0;
  color: var(--muted);
}
@media (max-width: 560px) {
  .accounts {
    grid-template-columns: 1fr;
  }
  .arrow {
    display: none;
  }
}
</style>
