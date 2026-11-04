<script setup lang="ts">
defineProps<{
  kind: 'error' | 'success' | 'info'
  message: string
}>()

defineEmits<{ dismiss: [] }>()
</script>

<template>
  <div class="alert" :class="kind" :role="kind === 'error' ? 'alert' : 'status'">
    <span>{{ message }}</span>
    <slot />
    <button type="button" class="dismiss" aria-label="Dismiss" @click="$emit('dismiss')">×</button>
  </div>
</template>

<style scoped>
.alert {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 0.75rem 1rem;
  border-radius: var(--radius);
  border: 1px solid;
}
.alert span {
  flex: 1;
}
.error {
  background: var(--danger-bg);
  border-color: var(--danger);
  color: var(--danger-text);
}
.success {
  background: var(--success-bg);
  border-color: var(--success);
  color: var(--success-text);
}
.info {
  background: var(--surface-2);
  border-color: var(--border);
}
.dismiss {
  background: none;
  border: none;
  font-size: 1.25rem;
  line-height: 1;
  color: inherit;
  cursor: pointer;
}
</style>
