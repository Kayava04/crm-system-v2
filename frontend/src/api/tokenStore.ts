/**
 * Access token lives only in memory (lost on reload, refetched via the refresh token).
 * Refresh token is persisted in localStorage — acceptable for this project; see README
 * for the trade-off (XSS could read it, but it can't survive as easily as a plain
 * always-valid session cookie since every access token is short-lived and rotated).
 */
const REFRESH_TOKEN_KEY = 'crm.refreshToken'

let accessToken: string | null = null
const listeners = new Set<() => void>()

function notify() {
  for (const l of listeners) l()
}

export const tokenStore = {
  getAccessToken: () => accessToken,
  getRefreshToken: () => localStorage.getItem(REFRESH_TOKEN_KEY),
  isAuthenticated: () => accessToken !== null || localStorage.getItem(REFRESH_TOKEN_KEY) !== null,

  setTokens(newAccessToken: string, newRefreshToken: string) {
    accessToken = newAccessToken
    localStorage.setItem(REFRESH_TOKEN_KEY, newRefreshToken)
    notify()
  },

  setAccessToken(newAccessToken: string) {
    accessToken = newAccessToken
    notify()
  },

  clear() {
    accessToken = null
    localStorage.removeItem(REFRESH_TOKEN_KEY)
    notify()
  },

  subscribe(listener: () => void) {
    listeners.add(listener)
    return () => listeners.delete(listener)
  },
}
