<script setup lang="ts">
import { computed, ref } from 'vue'
import { storeToRefs } from 'pinia'
import { useAccountsStore } from '@/stores/accounts'
import { useOperationForm } from '@/composables/useOperationForm'
import { validateAmount } from '@/utils/amountValidation'
import { depositReceipt, type Receipt } from '@/utils/receipts'
import AccountSelect from './AccountSelect.vue'
import AmountInput from './AmountInput.vue'
import AlertBanner from './AlertBanner.vue'

const accountId = defineModel<string>('accountId', { required: true })
const emit = defineEmits<{ completed: [receipt: Receipt] }>()

const store = useAccountsStore()
const { accounts, settings } = storeToRefs(store)

const amount = ref<number | null>(null)
const clientError = computed(() =>
  validateAmount(amount.value, { max: settings.value.maxTransactionAmount }),
)

const { pending, amountError, bannerError, dismissError, onSubmit } = useOperationForm({
  amount,
  clientError,
  submit: async () => depositReceipt(await store.deposit(accountId.value, amount.value!)),
  onCompleted: (receipt) => emit('completed', receipt),
})
</script>

<template>
  <form class="operation-form" novalidate @submit.prevent="onSubmit">
    <AccountSelect
      id="deposit-account"
      v-model="accountId"
      label="Deposit to"
      :accounts="accounts"
      :disabled="pending"
    />
    <AmountInput
      id="deposit-amount"
      v-model="amount"
      label="Amount"
      :error="amountError"
      :disabled="pending"
    />
    <AlertBanner v-if="bannerError" kind="error" :message="bannerError" @dismiss="dismissError" />
    <button type="submit" class="primary" :disabled="pending">
      {{ pending ? 'Processing…' : 'Deposit' }}
    </button>
  </form>
</template>
