import { describe, expect, it } from 'vitest'
import { ApiError, toApiError } from './errors'

function jsonResponse(status: number, body: unknown, headers: Record<string, string> = {}) {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/problem+json', ...headers },
  })
}

describe('ApiError', () => {
  it('prefers detail over title for the message', () => {
    const error = new ApiError(409, { title: 'Conflict', detail: 'Email already taken' })
    expect(error.message).toBe('Email already taken')
  })

  it('falls back to the title and then to a generic message with the status', () => {
    expect(new ApiError(404, { title: 'Not Found' }).message).toBe('Not Found')
    expect(new ApiError(500).message).toContain('500')
  })

  it('flags each status it knows about and only that one', () => {
    expect(new ApiError(401).isUnauthorized).toBe(true)
    expect(new ApiError(403).isForbidden).toBe(true)
    expect(new ApiError(404).isNotFound).toBe(true)
    expect(new ApiError(409).isConflict).toBe(true)
    expect(new ApiError(429).isRateLimited).toBe(true)
    expect(new ApiError(500).isUnauthorized).toBe(false)
  })

  it('treats a 400 as validation only when it carries field errors', () => {
    expect(new ApiError(400, { errors: { Email: ['Required'] } }).isValidation).toBe(true)
    expect(new ApiError(400, { errors: {} }).isValidation).toBe(false)
    expect(new ApiError(400).isValidation).toBe(false)
    expect(new ApiError(422, { errors: { Email: ['Required'] } }).isValidation).toBe(false)
  })
})

describe('toApiError', () => {
  it('reads the problem details from the body', async () => {
    const error = await toApiError(
      jsonResponse(400, { title: 'Invalid', detail: 'Bad input', errors: { Name: ['Too long'] } }),
    )
    expect(error).toBeInstanceOf(ApiError)
    expect(error.status).toBe(400)
    expect(error.detail).toBe('Bad input')
    expect(error.errors).toEqual({ Name: ['Too long'] })
  })

  it('parses Retry-After into seconds', async () => {
    const error = await toApiError(
      jsonResponse(429, { title: 'Too many' }, { 'Retry-After': '42' }),
    )
    expect(error.retryAfterSeconds).toBe(42)
  })

  it('leaves retryAfterSeconds null without the header', async () => {
    const error = await toApiError(jsonResponse(429, { title: 'Too many' }))
    expect(error.retryAfterSeconds).toBeNull()
  })

  it('survives a body that is not JSON', async () => {
    const error = await toApiError(new Response('<html>Bad gateway</html>', { status: 502 }))
    expect(error.status).toBe(502)
    expect(error.detail).toBeUndefined()
    expect(error.message).toContain('502')
  })

  it('does not consume the original response body', async () => {
    const response = jsonResponse(400, { detail: 'Bad input' })
    await toApiError(response)
    await expect(response.json()).resolves.toEqual({ detail: 'Bad input' })
  })
})
