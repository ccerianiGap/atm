<script setup lang="ts">
import { onMounted, onUnmounted, ref } from 'vue'
import type { Receipt } from '@/utils/receipts'
import { formatCurrency, formatDateTime } from '@/utils/format'

defineProps<{ receipt: Receipt }>()
const emit = defineEmits<{ close: [] }>()

const closeButton = ref<HTMLButtonElement>()
const onKeydown = (event: KeyboardEvent) => {
  if (event.key === 'Escape') emit('close')
}

onMounted(() => {
  closeButton.value?.focus()
  window.addEventListener('keydown', onKeydown)
})
onUnmounted(() => window.removeEventListener('keydown', onKeydown))
</script>

<template>
  <div class="backdrop" @click.self="emit('close')">
    <section class="receipt" role="dialog" aria-modal="true" aria-labelledby="receipt-title">
      <p class="check" aria-hidden="true">✓</p>
      <h2 id="receipt-title">{{ receipt.title }}</h2>
      <p class="amount">{{ formatCurrency(receipt.amount) }}</p>
      <dl>
        <template v-for="line in receipt.lines" :key="line.label">
          <dt>{{ line.label }}</dt>
          <dd>{{ line.value }}</dd>
        </template>
        <dt>Reference</dt>
        <dd>{{ receipt.reference }}</dd>
        <dt>Date</dt>
        <dd>{{ formatDateTime(receipt.createdAtUtc) }}</dd>
      </dl>
      <button ref="closeButton" type="button" class="primary" @click="emit('close')">Done</button>
    </section>
  </div>
</template>

<style scoped>
.backdrop {
  position: fixed;
  inset: 0;
  display: grid;
  place-items: center;
  padding: 1rem;
  background: rgb(0 0 0 / 0.45);
  z-index: 10;
}
.receipt {
  width: min(24rem, 100%);
  padding: 1.5rem;
  border-radius: var(--radius-lg);
  background: var(--surface);
  text-align: center;
  box-shadow: 0 20px 50px rgb(0 0 0 / 0.25);
}
.check {
  display: inline-grid;
  place-items: center;
  width: 3rem;
  height: 3rem;
  margin: 0 auto;
  border-radius: 50%;
  background: var(--success-bg);
  color: var(--success);
  font-size: 1.5rem;
  font-weight: 700;
}
h2 {
  margin: 0.75rem 0 0;
  font-size: 1.15rem;
}
.amount {
  margin: 0.25rem 0 1rem;
  font-size: 2rem;
  font-weight: 700;
  font-variant-numeric: tabular-nums;
}
dl {
  display: grid;
  grid-template-columns: auto 1fr;
  gap: 0.4rem 1rem;
  margin: 0 0 1.25rem;
  padding-top: 1rem;
  border-top: 1px dashed var(--border);
  text-align: left;
  font-size: 0.9rem;
}
dt {
  color: var(--muted);
}
dd {
  margin: 0;
  text-align: right;
  font-variant-numeric: tabular-nums;
}
button {
  width: 100%;
}
</style>
