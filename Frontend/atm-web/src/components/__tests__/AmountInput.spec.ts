import { describe, expect, it } from 'vitest'
import { mount, type VueWrapper } from '@vue/test-utils'
import AmountInput from '../AmountInput.vue'

function mountInput(allowDecimals = true) {
  return mount(AmountInput, {
    props: {
      id: 'amount',
      label: 'Amount',
      allowDecimals,
      modelValue: null,
      'onUpdate:modelValue': () => {},
    },
  })
}

const lastEmitted = (wrapper: VueWrapper) => {
  const events = wrapper.emitted('update:modelValue') ?? []
  return events[events.length - 1]
}

describe('AmountInput', () => {
  it('limits input to two decimals', async () => {
    const wrapper = mountInput()

    await wrapper.find('input').setValue('12.345')

    expect(lastEmitted(wrapper)).toEqual([12.34])
    expect((wrapper.find('input').element as HTMLInputElement).value).toBe('12.34')
  })

  it('drops the minus sign and other characters', async () => {
    const wrapper = mountInput()

    await wrapper.find('input').setValue('-4a0')

    expect(lastEmitted(wrapper)).toEqual([40])
  })

  it('accepts only whole numbers when decimals are not allowed', async () => {
    const wrapper = mountInput(false)

    await wrapper.find('input').setValue('25.5')

    expect(lastEmitted(wrapper)).toEqual([255])
  })

  it('emits null when cleared', async () => {
    const wrapper = mountInput()
    const input = wrapper.find('input')

    await input.setValue('15')
    await input.setValue('')

    expect(lastEmitted(wrapper)).toEqual([null])
  })
})
