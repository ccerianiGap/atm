import { ref } from 'vue'
import { ApiError } from '@/api/httpClient'

/**
 * Wraps an async operation with `pending` / `error` state so components don't repeat
 * try/catch/finally. `run` resolves to undefined when the operation fails.
 */
export function useAsyncAction<TArgs extends unknown[], TResult>(
  action: (...args: TArgs) => Promise<TResult>,
) {
  const pending = ref(false)
  const error = ref<ApiError | null>(null)

  async function run(...args: TArgs): Promise<TResult | undefined> {
    pending.value = true
    error.value = null
    try {
      return await action(...args)
    } catch (e) {
      error.value = ApiError.from(e)
      return undefined
    } finally {
      pending.value = false
    }
  }

  const reset = () => {
    error.value = null
  }

  return { pending, error, run, reset }
}
