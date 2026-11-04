<script setup lang="ts">
import { ref, watch } from 'vue'

const props = withDefaults(
  defineProps<{
    id: string
    label: string
    hint?: string
    error?: string | null
    allowDecimals?: boolean
    disabled?: boolean
  }>(),
  { allowDecimals: true },
)

const model = defineModel<number | null>({ required: true })

// Text is kept separately so partial input like "12." is not lost while typing.
const text = ref(model.value?.toString() ?? '')

watch(model, (value) => {
  if (value === null) {
    text.value = ''
  } else if (Number(text.value) !== value) {
    text.value = value.toString()
  }
})

/** Keeps digits and (optionally) one decimal point with up to 2 decimals. Signs are dropped. */
function sanitize(raw: string): string {
  const digitsAndDot = raw.replace(props.allowDecimals ? /[^\d.]/g : /\D/g, '')
  const [whole = '', ...rest] = digitsAndDot.split('.')
  return rest.length === 0 ? whole : `${whole}.${rest.join('').slice(0, 2)}`
}

function onInput(event: Event) {
  const input = event.target as HTMLInputElement
  const clean = sanitize(input.value)
  input.value = clean
  text.value = clean
  model.value = clean === '' || clean === '.' ? null : Number(clean)
}
</script>

<template>
  <div class="field">
    <label :for="id">{{ label }}</label>
    <div class="amount" :class="{ invalid: !!error }">
      <span class="prefix" aria-hidden="true">$</span>
      <input
        :id="id"
        :value="text"
        type="text"
        :inputmode="allowDecimals ? 'decimal' : 'numeric'"
        autocomplete="off"
        placeholder="0.00"
        :disabled="disabled"
        :aria-invalid="!!error"
        :aria-describedby="`${id}-help`"
        @input="onInput"
      />
    </div>
    <small :id="`${id}-help`" :class="{ 'field-error': !!error }">{{ error || hint }}</small>
  </div>
</template>

<style scoped>
.amount {
  display: flex;
  align-items: center;
  border: 1px solid var(--border);
  border-radius: var(--radius);
  background: var(--surface);
  padding-inline: 0.75rem;
}
.amount:focus-within {
  outline: 2px solid var(--primary);
  outline-offset: 1px;
}
.amount.invalid {
  border-color: var(--danger);
}
.prefix {
  color: var(--muted);
  font-weight: 600;
}
input {
  flex: 1;
  border: none;
  outline: none;
  background: transparent;
  padding: 0.65rem 0.5rem;
  font-size: 1.25rem;
  font-variant-numeric: tabular-nums;
  color: inherit;
  min-width: 0;
}
</style>
