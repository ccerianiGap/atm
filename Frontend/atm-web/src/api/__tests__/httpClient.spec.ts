import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError, request } from '../httpClient'

const jsonResponse = (status: number, body: unknown) =>
  new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/problem+json' },
  })

describe('request', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('returns the parsed body on success', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(jsonResponse(200, { ok: true })))

    await expect(request('/api/x')).resolves.toEqual({ ok: true })
  })

  it('maps a ProblemDetails response to ApiError', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(
        jsonResponse(400, {
          title: 'Validation failed',
          detail: 'Withdrawals must be in multiples of 20.',
          code: 'validation_failed',
          errors: { amount: ['Withdrawals must be in multiples of 20.'] },
        }),
      ),
    )

    const error = await request('/api/x').catch((e: unknown) => e)

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({
      status: 400,
      code: 'validation_failed',
      message: 'Withdrawals must be in multiples of 20.',
      fieldErrors: { amount: ['Withdrawals must be in multiples of 20.'] },
    })
  })

  it('maps a network failure to a friendly ApiError', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('Failed to fetch')))

    await expect(request('/api/x')).rejects.toMatchObject({ status: 0, code: 'network_error' })
  })
})
