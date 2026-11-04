import { computed, ref, type Ref } from 'vue'
import { useAsyncAction } from './useAsyncAction'
import type { Receipt } from '@/utils/receipts'

/**
 * Shared submit/validation flow for the operation forms:
 * - client-side errors are shown once the user has typed or tried to submit
 * - server field errors are shown inline; other server errors go to a banner
 * - the form resets and emits a receipt on success
 */
export function useOperationForm(options: {
  amount: Ref<number | null>
  clientError: Ref<string | null>
  submit: () => Promise<Receipt>
  onCompleted: (receipt: Receipt) => void
}) {
  const attempted = ref(false)
  const { pending, error, run, reset } = useAsyncAction(options.submit)

  const amountError = computed(() => {
    const showClient = attempted.value || options.amount.value !== null
    return (
      (showClient ? options.clientError.value : null) ??
      error.value?.fieldErrors.amount?.[0] ??
      null
    )
  })

  const bannerError = computed(() =>
    error.value && Object.keys(error.value.fieldErrors).length === 0 ? error.value.message : null,
  )

  async function onSubmit() {
    attempted.value = true
    if (options.clientError.value || pending.value) {
      return
    }
    const receipt = await run()
    if (receipt) {
      options.amount.value = null
      attempted.value = false
      options.onCompleted(receipt)
    }
  }

  return { pending, amountError, bannerError, dismissError: reset, onSubmit }
}
