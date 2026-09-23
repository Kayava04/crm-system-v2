import createClient, { type Middleware } from 'openapi-fetch'
import type { paths } from './schema'
import { tokenStore } from './tokenStore'
import { toApiError } from './errors'
import { createSingleFlight } from './singleFlight'

// Empty by default: every call is a relative /api/... path, resolved by the browser
// against whatever origin served the page. nginx (Docker) and the Vite dev server
// (see vite.config.ts) both proxy that path to the backend, so the same build works
// unchanged from localhost, a phone on the LAN, or a real domain — nothing to
// configure per environment or device.
export const API_BASE_URL = import.meta.env.VITE_API_URL ?? ''

const runRefresh = createSingleFlight<string | null>()

async function performRefresh(): Promise<string | null> {
  const refreshToken = tokenStore.getRefreshToken()
  if (!refreshToken) return null
  try {
    const res = await fetch(`${API_BASE_URL}/api/auth/refresh`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ refreshToken }),
    })
    if (!res.ok) return null
    const data = (await res.json()) as { accessToken: string; refreshToken: string }
    tokenStore.setTokens(data.accessToken, data.refreshToken)
    return data.accessToken
  } catch {
    return null
  }
}

// A pristine clone of every outgoing request, taken before it is sent, so a 401
// can be retried once with a fresh access token even for requests with a body
// (a Request's body can only be read once).
const pristineRequests = new WeakMap<Request, Request>()

const authMiddleware: Middleware = {
  async onRequest({ request }) {
    const token = tokenStore.getAccessToken()
    if (token) request.headers.set('Authorization', `Bearer ${token}`)
    pristineRequests.set(request, request.clone())
    return request
  },

  async onResponse({ request, response }) {
    if (response.status !== 401) return response

    // Endpoints that don't need/use a session (login) or that ARE the refresh
    // call itself never trigger a refresh loop.
    const url = new URL(request.url)
    if (url.pathname === '/api/auth/login' || url.pathname === '/api/auth/refresh') {
      return response
    }
    // Already retried once — give up rather than loop forever.
    if (request.headers.get('X-Retry')) {
      tokenStore.clear()
      return response
    }
    if (!tokenStore.getRefreshToken()) {
      return response
    }

    const newAccessToken = await runRefresh(performRefresh)
    if (!newAccessToken) {
      tokenStore.clear()
      return response
    }

    const pristine = pristineRequests.get(request)
    if (!pristine) {
      tokenStore.clear()
      return response
    }
    const retryRequest = new Request(pristine, { headers: new Headers(pristine.headers) })
    retryRequest.headers.set('Authorization', `Bearer ${newAccessToken}`)
    retryRequest.headers.set('X-Retry', '1')
    return fetch(retryRequest)
  },
}

export const api = createClient<paths>({ baseUrl: API_BASE_URL })
api.use(authMiddleware)

/** Unwrap an openapi-fetch result, throwing a normalized ApiError on failure. */
export async function unwrap<T>(
  result: Promise<{ data?: T; error?: unknown; response: Response }>,
): Promise<T> {
  const { data, response } = await result
  if (!response.ok) {
    throw await toApiError(response)
  }
  return data as T
}
