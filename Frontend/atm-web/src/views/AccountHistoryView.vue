<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useAccountsStore } from '@/stores/accounts'
import { accountsApi } from '@/api/accountsApi'
import type { PagedResult, Transaction } from '@/api/types'
import { useAsyncAction } from '@/composables/useAsyncAction'
import { formatCurrency, maskAccountNumber } from '@/utils/format'
import TransactionTable from '@/components/TransactionTable.vue'
import AlertBanner from '@/components/AlertBanner.vue'

const PAGE_SIZE = 10

const props = defineProps<{ id: string }>()

const store = useAccountsStore()
const account = computed(() => store.accountById(props.id))

const page = ref(1)
const history = ref<PagedResult<Transaction> | null>(null)
const { pending, error, run } = useAsyncAction(async () => {
  history.value = await accountsApi.getTransactions(props.id, page.value, PAGE_SIZE)
})

watch(
  () => props.id,
  () => (page.value = 1),
)
watch([() => props.id, page], () => run(), { immediate: true })
</script>

<template>
  <div class="history">
    <RouterLink :to="{ name: 'dashboard' }" class="back">← Back to accounts</RouterLink>

    <header v-if="account" class="header">
      <div>
        <h1>{{ account.name }}</h1>
        <p class="number">{{ maskAccountNumber(account.accountNumber) }}</p>
      </div>
      <div class="balance">
        <span>Balance</span>
        <strong>{{ formatCurrency(account.balance) }}</strong>
      </div>
      <RouterLink class="button-link" :to="{ name: 'dashboard', query: { account: account.id } }">
        New operation
      </RouterLink>
    </header>

    <section class="panel" aria-labelledby="transactions-heading">
      <h2 id="transactions-heading">Transactions</h2>

      <AlertBanner v-if="error" kind="error" :message="error.message" @dismiss="error = null">
        <button type="button" class="link-button" @click="run()">Retry</button>
      </AlertBanner>
      <p v-else-if="pending && !history" class="muted">Loading transactions…</p>
      <p v-else-if="history && history.items.length === 0" class="muted">No transactions yet.</p>

      <template v-if="history && history.items.length > 0">
        <TransactionTable :transactions="history.items" :class="{ stale: pending }" />
        <nav class="pager" aria-label="Pagination">
          <button type="button" :disabled="page <= 1 || pending" @click="page--">Previous</button>
          <span>Page {{ history.page }} of {{ history.totalPages }}</span>
          <button type="button" :disabled="page >= history.totalPages || pending" @click="page++">
            Next
          </button>
        </nav>
      </template>
    </section>
  </div>
</template>

<style scoped>
.history {
  display: grid;
  gap: 1.25rem;
}
.back {
  justify-self: start;
}
.header {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 1rem 2rem;
}
.header > div:first-child {
  flex: 1;
}
h1 {
  margin: 0;
  font-size: 1.5rem;
}
.number {
  margin: 0;
  color: var(--muted);
}
.balance {
  display: grid;
  text-align: right;
}
.balance span {
  color: var(--muted);
  font-size: 0.8rem;
  text-transform: uppercase;
}
.balance strong {
  font-size: 1.5rem;
  font-variant-numeric: tabular-nums;
}
.panel {
  padding: 1.25rem;
  border: 1px solid var(--border);
  border-radius: var(--radius-lg);
  background: var(--surface);
}
h2 {
  margin: 0 0 1rem;
  font-size: 1.1rem;
}
.muted {
  color: var(--muted);
}
.stale {
  opacity: 0.6;
}
.pager {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 1rem;
  margin-top: 1rem;
}
</style>
