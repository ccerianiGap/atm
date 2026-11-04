<script setup lang="ts">
import { onMounted } from 'vue'
import { storeToRefs } from 'pinia'
import { useAccountsStore } from '@/stores/accounts'
import AlertBanner from '@/components/AlertBanner.vue'

const store = useAccountsStore()
const { accounts, loading, error } = storeToRefs(store)

onMounted(() => {
  store.fetchAccounts()
  store.fetchSettings()
})
</script>

<template>
  <header class="app-header">
    <div class="container">
      <RouterLink :to="{ name: 'dashboard' }" class="brand">
        <span class="logo" aria-hidden="true">$</span> ATM
      </RouterLink>
    </div>
  </header>

  <main class="container">
    <AlertBanner
      v-if="error"
      kind="error"
      :message="`Could not load your accounts. ${error.message}`"
      @dismiss="error = null"
    >
      <button type="button" class="link-button" @click="store.fetchAccounts()">Retry</button>
    </AlertBanner>
    <p v-else-if="loading && accounts.length === 0" class="loading">Loading accounts…</p>
    <RouterView v-if="accounts.length > 0" />
  </main>
</template>

<style scoped>
.app-header {
  background: var(--header-bg);
  color: var(--on-primary);
}
.app-header .container {
  padding-block: 0.9rem;
}
.brand {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  color: inherit;
  font-size: 1.15rem;
  font-weight: 700;
  letter-spacing: 0.05em;
  text-decoration: none;
}
.logo {
  display: inline-grid;
  place-items: center;
  width: 1.9rem;
  height: 1.9rem;
  border-radius: 0.5rem;
  background: var(--primary);
}
main {
  display: grid;
  gap: 1rem;
  padding-block: 2rem 3rem;
}
.loading {
  color: var(--muted);
}
</style>
