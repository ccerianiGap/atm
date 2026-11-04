import { describe, expect, it } from 'vitest'
import { validateAmount } from '../amountValidation'

describe('validateAmount', () => {
  const rules = { max: 10_000 }

  it.each([0.01, 0.29, 19.99, 10_000])('accepts %s', (amount) => {
    expect(validateAmount(amount, rules)).toBeNull()
  })

  it.each([
    [null, 'Enter an amount.'],
    [0, 'greater than zero'],
    [-5, 'greater than zero'],
    [1.005, '2 decimal places'],
    [10_000.01, 'cannot exceed'],
  ])('rejects %s', (amount, message) => {
    expect(validateAmount(amount, rules)).toContain(message)
  })

  it('enforces the withdrawal multiple when given', () => {
    expect(validateAmount(60, { ...rules, multiple: 20 })).toBeNull()
    expect(validateAmount(50, { ...rules, multiple: 20 })).toContain('multiple of $20.00')
  })

  it('rejects amounts above the available balance', () => {
    expect(validateAmount(100, { ...rules, available: 99.99 })).toContain('Insufficient funds')
    expect(validateAmount(99.99, { ...rules, available: 99.99 })).toBeNull()
  })
})
