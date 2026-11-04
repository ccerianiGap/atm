import { formatCurrency } from './format'

export interface AmountRules {
  max: number
  /** Balance of the account being debited; omit for deposits. */
  available?: number
  /** Amount must be a multiple of this value; 0 or omitted disables the rule. */
  multiple?: number
}

/**
 * Mirrors the server rules for instant feedback. The server remains the source of truth.
 * Returns an error message, or null when the amount is valid.
 */
export function validateAmount(amount: number | null, rules: AmountRules): string | null {
  if (amount === null || Number.isNaN(amount)) {
    return 'Enter an amount.'
  }
  if (amount <= 0) {
    return 'Amount must be greater than zero.'
  }
  // Tolerance absorbs binary floating-point noise (0.29 * 100 === 28.999999999999996).
  if (Math.abs(Math.round(amount * 100) - amount * 100) > 1e-6) {
    return 'Amount can have at most 2 decimal places.'
  }
  if (amount > rules.max) {
    return `Amount cannot exceed ${formatCurrency(rules.max)} per operation.`
  }
  if (rules.multiple && amount % rules.multiple !== 0) {
    return `Amount must be a multiple of ${formatCurrency(rules.multiple)}.`
  }
  if (rules.available !== undefined && amount > rules.available) {
    return `Insufficient funds. Available: ${formatCurrency(rules.available)}.`
  }
  return null
}
