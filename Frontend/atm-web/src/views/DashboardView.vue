<script setup lang="ts">
import { computed, nextTick, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { storeToRefs } from 'pinia'
import { useAccountsStore } from '@/stores/accounts'
import { isOperationTab, type OperationTab } from '@/components/operations'
import AccountCard from '@/components/AccountCard.vue'
import OperationPanel from '@/components/OperationPanel.vue'

const route = useRoute()
const router = useRouter()
const { accounts } = storeToRefs(useAccountsStore())
const panel = ref<HTMLElement>()

// The URL (?account=<id>&op=<tab>) is the source of truth for the selection, so it
// survives a refresh and other pages can link straight to an operation.
const selectedAccountId = computed<string>({
  get: () => {
    const fromQuery = route.query.account
    return accounts.value.some((a) => a.id === fromQuery)
      ? (fromQuery as string)
      : (accounts.value[0]?.id ?? '')
  },
  set: (account) => updateQuery({ account }),
})

const activeTab = computed<OperationTab>({
  get: () => (isOperationTab(route.query.op) ? route.query.op : 'deposit'),
  set: (op) => updateQuery({ op }),
})

function updateQuery(changes: { account?: string; op?: OperationTab }) {
  router.replace({ query: { ...route.query, ...changes } })
}

async function selectAccount(accountId: string, op?: OperationTab) {
  updateQuery(op ? { account: accountId, op } : { account: accountId })
  await nextTick()
  panel.value?.scrollIntoView({ behavior: 'smooth', block: 'nearest' })
}
</script>

<template>
  <div class="dashboard">
    <section aria-labelledby="accounts-heading">
      <h1 id="accounts-heading">Your accounts</h1>
      <p class="hint">Select an account to use it in the operations below.</p>
      <div class="cards">
        <AccountCard
          v-for="account in accounts"
          :key="account.id"
          :account="account"
          :selected="account.id === selectedAccountId"
          @select="selectAccount(account.id)"
          @action="selectAccount(account.id, $event)"
        />
      </div>
    </section>

    <div ref="panel">
      <OperationPanel
        v-if="selectedAccountId"
        v-model:tab="activeTab"
        v-model:account-id="selectedAccountId"
      />
    </div>
  </div>
</template>

<style scoped>
.dashboard {
  display: grid;
  gap: 2rem;
}
h1 {
  margin: 0;
  font-size: 1.5rem;
}
.hint {
  margin: 0.25rem 0 1rem;
  color: var(--muted);
}
.cards {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(17rem, 1fr));
  gap: 1rem;
}
</style>
