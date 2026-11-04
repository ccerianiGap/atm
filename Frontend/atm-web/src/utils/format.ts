const currency = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' })
const dateTime = new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' })

export const formatCurrency = (amount: number): string => currency.format(amount)

export const formatDateTime = (isoUtc: string): string => dateTime.format(new Date(isoUtc))

/** "1000-0001" -> "•••• 0001" */
export const maskAccountNumber = (accountNumber: string): string =>
  `•••• ${accountNumber.slice(-4)}`
