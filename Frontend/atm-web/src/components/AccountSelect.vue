<script setup lang="ts">
import type { Account } from '@/api/types'
import { formatCurrency, maskAccountNumber } from '@/utils/format'

defineProps<{
  id: string
  label: string
  accounts: Account[]
  disabled?: boolean
}>()

const model = defineModel<string>({ required: true })
</script>

<template>
  <div class="field">
    <label :for="id">{{ label }}</label>
    <select :id="id" v-model="model" :disabled="disabled">
      <option v-for="account in accounts" :key="account.id" :value="account.id">
        {{ account.name }} {{ maskAccountNumber(account.accountNumber) }} —
        {{ formatCurrency(account.balance) }}
      </option>
    </select>
  </div>
</template>
