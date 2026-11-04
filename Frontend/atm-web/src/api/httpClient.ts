import type { ProblemDetails } from './types'

/** The single error type the rest of the app deals with. */
export class ApiError extends Error {
  constructor(
    message: string,
    readonly status: number,
    readonly code?: string,
    readonly fieldErrors: Record<string, string[]> = {},
  ) {
    super(message)
    this.name = 'ApiError'
  }

  get isConflict(): boolean {
    return this.status === 409
  }

  static from(error: unknown): ApiError {
    return error instanceof ApiError
      ? error
      : new ApiError('Something went wrong. Please try again.', 0, 'unknown_error')
  }
}

export async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  let response: Response
  try {
    response = await fetch(path, {
      ...init,
      headers: { Accept: 'application/json', 'Content-Type': 'application/json', ...init.headers },
    })
  } catch {
    throw new ApiError(
      'Unable to reach the server. Check your connection and try again.',
      0,
      'network_error',
    )
  }

  if (!response.ok) {
    throw await toApiError(response)
  }

  return (await response.json()) as T
}

async function toApiError(response: Response): Promise<ApiError> {
  let problem: ProblemDetails = {}
  try {
    problem = (await response.json()) as ProblemDetails
  } catch {
    // Not a ProblemDetails body (e.g. a proxy error page); fall back to the status text.
  }

  return new ApiError(
    problem.detail ?? problem.title ?? `Request failed (${response.status} ${response.statusText})`,
    response.status,
    problem.code,
    problem.errors ?? {},
  )
}
