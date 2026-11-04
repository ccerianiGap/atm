import { beforeEach, describe, expect, it, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import type { accountsApi } from '@/api/accountsApi'
import { useAccountsStore } from '@/stores/accounts'
import WithdrawForm from '../WithdrawForm.vue'

vi.mock('@/api/accountsApi', () => ({
  accountsApi: { withdraw: vi.fn<(typeof accountsApi)['withdraw']>() },
}))

describe('WithdrawForm', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    useAccountsStore().accounts = [
      { id: 'c', accountNumber: '1000-0001', name: 'Checking', balance: 150 },
      { id: 's', accountNumber: '1000-0002', name: 'Savings', balance: 5000 },
    ]
  })

  const mountForm = (accountId = 'c') =>
    mount(WithdrawForm, { props: { accountId, 'onUpdate:accountId': () => {} } })

  it('uses the selected account as the source', () => {
    const wrapper = mountForm('s')

    expect((wrapper.find('select').element as HTMLSelectElement).value).toBe('s')
  })

  it('disables quick amounts above the available balance', () => {
    const wrapper = mountForm('c')
    const tile = (label: string) => wrapper.findAll('.tile').find((b) => b.text() === label)!

    expect(tile('$100.00').attributes('disabled')).toBeUndefined()
    expect(tile('$200.00').attributes('disabled')).toBeDefined()
  })

  it('shows a custom amount field for "Other amount" and rejects non-multiples of 20', async () => {
    const wrapper = mountForm('c')
    expect(wrapper.find('#withdraw-amount').exists()).toBe(false)

    await wrapper.find('[data-testid="other-amount"]').trigger('click')
    await wrapper.find('#withdraw-amount').setValue('50')

    expect(wrapper.text()).toContain('Amount must be a multiple of $20.00')
  })
})
