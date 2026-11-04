export const OPERATION_TABS = ['deposit', 'withdraw', 'transfer'] as const

export type OperationTab = (typeof OPERATION_TABS)[number]

export const isOperationTab = (value: unknown): value is OperationTab =>
  typeof value === 'string' && (OPERATION_TABS as readonly string[]).includes(value)
