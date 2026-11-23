import { describe, expect, it, vi, beforeEach, afterEach, type Mock } from 'vitest'

type Handler = (request: Request) => Response | Promise<Response>

let fetchMock: Mock
let client: typeof import('./client')
let tokenStore: typeof import('./tokenStore').tokenStore
let ApiError: typeof import('./errors').ApiError

function json(status: number, body: unknown) {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  })
}

function route(handlers: Record<string, Handler>) {
  fetchMock.mockImplementation(async (input: RequestInfo | URL, init?: RequestInit) => {
    const request = input instanceof Request ? input : new Request(input, init)
    const handler = handlers[new URL(request.url).pathname]
    if (!handler) throw new Error(`Unexpected request to ${request.url}`)
    return handler(request)
  })
}

function callsTo(pathname: string) {
  return fetchMock.mock.calls
    .map(([input, init]) => (input instanceof Request ? input : new Request(input, init)))
    .filter((r) => new URL(r.url).pathname === pathname)
}

beforeEach(async () => {
  vi.resetModules()
  vi.stubEnv('VITE_API_URL', 'http://api.test')
  fetchMock = vi.fn()
  vi.stubGlobal('fetch', fetchMock)
  localStorage.clear()

  client = await import('./client')
  tokenStore = (await import('./tokenStore')).tokenStore
  ApiError = (await import('./errors')).ApiError
})

afterEach(() => {
  vi.unstubAllEnvs()
  vi.unstubAllGlobals()
})

describe('api client auth middleware', () => {
  it('sends the access token as a bearer header', async () => {
    tokenStore.setTokens('access-1', 'refresh-1')
    route({ '/api/auth/me': () => json(200, { userId: 'u1' }) })

    await client.api.GET('/api/auth/me', {})

    expect(callsTo('/api/auth/me')[0].headers.get('Authorization')).toBe('Bearer access-1')
  })

  it('sends no Authorization header without an access token', async () => {
    route({ '/api/auth/me': () => json(200, { userId: 'u1' }) })

    await client.api.GET('/api/auth/me', {})

    expect(callsTo('/api/auth/me')[0].headers.has('Authorization')).toBe(false)
  })

  it('refreshes on a 401 and retries the request once with the new token', async () => {
    tokenStore.setTokens('expired', 'refresh-1')
    route({
      '/api/auth/me': (r) =>
        r.headers.get('Authorization') === 'Bearer access-2'
          ? json(200, { userId: 'u1' })
          : json(401, {}),
      '/api/auth/refresh': () => json(200, { accessToken: 'access-2', refreshToken: 'refresh-2' }),
    })

    const { data, response } = await client.api.GET('/api/auth/me', {})

    expect(response.status).toBe(200)
    expect(data).toEqual({ userId: 'u1' })
    expect(tokenStore.getAccessToken()).toBe('access-2')
    expect(tokenStore.getRefreshToken()).toBe('refresh-2')
    const [, retry] = callsTo('/api/auth/me')
    expect(retry.headers.get('X-Retry')).toBe('1')
  })

  it('sends the stored refresh token in the refresh request', async () => {
    tokenStore.setTokens('expired', 'refresh-1')
    route({
      '/api/auth/me': (r) => (r.headers.get('X-Retry') ? json(200, {}) : json(401, {})),
      '/api/auth/refresh': () => json(200, { accessToken: 'access-2', refreshToken: 'refresh-2' }),
    })

    await client.api.GET('/api/auth/me', {})

    await expect(callsTo('/api/auth/refresh')[0].json()).resolves.toEqual({
      refreshToken: 'refresh-1',
    })
  })

  it('runs a single refresh for parallel requests that all hit a 401', async () => {
    tokenStore.setTokens('expired', 'refresh-1')
    route({
      '/api/auth/me': (r) =>
        r.headers.get('Authorization') === 'Bearer access-2' ? json(200, {}) : json(401, {}),
      '/api/auth/refresh': () => json(200, { accessToken: 'access-2', refreshToken: 'refresh-2' }),
    })

    const results = await Promise.all([
      client.api.GET('/api/auth/me', {}),
      client.api.GET('/api/auth/me', {}),
      client.api.GET('/api/auth/me', {}),
    ])

    expect(results.map((r) => r.response.status)).toEqual([200, 200, 200])
    expect(callsTo('/api/auth/refresh')).toHaveLength(1)
  })

  it('clears the session and returns the 401 when the refresh is rejected', async () => {
    tokenStore.setTokens('expired', 'refresh-1')
    route({
      '/api/auth/me': () => json(401, {}),
      '/api/auth/refresh': () => json(401, {}),
    })

    const { response } = await client.api.GET('/api/auth/me', {})

    expect(response.status).toBe(401)
    expect(tokenStore.isAuthenticated()).toBe(false)
  })

  it('clears the session when the refresh request cannot reach the server', async () => {
    tokenStore.setTokens('expired', 'refresh-1')
    route({
      '/api/auth/me': () => json(401, {}),
      '/api/auth/refresh': () => Promise.reject(new TypeError('Failed to fetch')),
    })

    const { response } = await client.api.GET('/api/auth/me', {})

    expect(response.status).toBe(401)
    expect(tokenStore.isAuthenticated()).toBe(false)
  })

  it('gives up after one retry when the new token is rejected too', async () => {
    tokenStore.setTokens('expired', 'refresh-1')
    route({
      '/api/auth/me': () => json(401, {}),
      '/api/auth/refresh': () => json(200, { accessToken: 'access-2', refreshToken: 'refresh-2' }),
    })

    const { response } = await client.api.GET('/api/auth/me', {})

    expect(response.status).toBe(401)
    expect(callsTo('/api/auth/me')).toHaveLength(2)
    expect(callsTo('/api/auth/refresh')).toHaveLength(1)
  })

  it('does not try to refresh without a refresh token', async () => {
    route({ '/api/auth/me': () => json(401, {}) })

    const { response } = await client.api.GET('/api/auth/me', {})

    expect(response.status).toBe(401)
    expect(callsTo('/api/auth/refresh')).toHaveLength(0)
  })

  it('never refreshes a failed login', async () => {
    tokenStore.setTokens('access-1', 'refresh-1')
    route({ '/api/auth/login': () => json(401, { detail: 'Invalid credentials' }) })

    const { response } = await client.api.POST('/api/auth/login', {
      body: { email: 'a@example.com', password: 'wrong' },
    })

    expect(response.status).toBe(401)
    expect(callsTo('/api/auth/refresh')).toHaveLength(0)
    expect(tokenStore.getRefreshToken()).toBe('refresh-1')
  })

  it('replays the original request body on retry', async () => {
    tokenStore.setTokens('expired', 'refresh-1')
    route({
      '/api/auth/change-password': (r) =>
        r.headers.get('X-Retry') ? json(200, {}) : json(401, {}),
      '/api/auth/refresh': () => json(200, { accessToken: 'access-2', refreshToken: 'refresh-2' }),
    })
    const body = {
      oldPassword: 'old',
      newPassword: 'new-password',
      confirmNewPassword: 'new-password',
    }

    await client.api.POST('/api/auth/change-password', { body })

    const [, retry] = callsTo('/api/auth/change-password')
    await expect(retry.json()).resolves.toEqual(body)
  })
})

describe('unwrap', () => {
  it('returns the data of a successful response', async () => {
    route({ '/api/auth/me': () => json(200, { userId: 'u1' }) })
    await expect(client.unwrap(client.api.GET('/api/auth/me', {}))).resolves.toEqual({
      userId: 'u1',
    })
  })

  it('throws an ApiError carrying the response status', async () => {
    route({ '/api/auth/me': () => json(404, { title: 'Not Found' }) })

    const error = await client.unwrap(client.api.GET('/api/auth/me', {})).catch((e) => e)

    expect(error).toBeInstanceOf(ApiError)
    expect(error.status).toBe(404)
    expect(error.isNotFound).toBe(true)
  })
})
