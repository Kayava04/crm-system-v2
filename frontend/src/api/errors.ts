export interface ProblemDetails {
  type?: string | null
  title?: string | null
  status?: number | string | null
  detail?: string | null
  instance?: string | null
}

export interface ValidationProblemDetails extends ProblemDetails {
  errors?: Record<string, string[]>
}

/** A normalized API error, built from RFC 9457 problem+json (or a network failure). */
export class ApiError extends Error {
  readonly status: number
  readonly title?: string | null
  readonly detail?: string | null
  readonly errors?: Record<string, string[]>
  readonly retryAfterSeconds?: number | null

  constructor(
    status: number,
    problem?: ValidationProblemDetails,
    retryAfterSeconds?: number | null,
  ) {
    super(problem?.detail || problem?.title || `Помилка запиту (${status})`)
    this.name = 'ApiError'
    this.status = status
    this.title = problem?.title
    this.detail = problem?.detail
    this.errors = problem?.errors
    this.retryAfterSeconds = retryAfterSeconds
  }

  get isValidation() {
    return this.status === 400 && !!this.errors && Object.keys(this.errors).length > 0
  }
  get isUnauthorized() {
    return this.status === 401
  }
  get isForbidden() {
    return this.status === 403
  }
  get isNotFound() {
    return this.status === 404
  }
  get isConflict() {
    return this.status === 409
  }
  get isRateLimited() {
    return this.status === 429
  }
}

export async function toApiError(response: Response): Promise<ApiError> {
  let problem: ValidationProblemDetails | undefined
  try {
    problem = await response.clone().json()
  } catch {
    problem = undefined
  }
  const retryAfterHeader = response.headers.get('Retry-After')
  return new ApiError(response.status, problem, retryAfterHeader ? Number(retryAfterHeader) : null)
}
