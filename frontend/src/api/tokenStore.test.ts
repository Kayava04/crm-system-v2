import { describe, expect, it, vi, beforeEach } from 'vitest'
import { tokenStore } from './tokenStore'

beforeEach(() => {
  tokenStore.clear()
  localStorage.clear()
})

describe('tokenStore', () => {
  it('starts unauthenticated with no tokens', () => {
    expect(tokenStore.getAccessToken()).toBeNull()
    expect(tokenStore.getRefreshToken()).toBeNull()
    expect(tokenStore.isAuthenticated()).toBe(false)
  })

  it('keeps the access token in memory and the refresh token in localStorage', () => {
    tokenStore.setTokens('access-1', 'refresh-1')

    expect(tokenStore.getAccessToken()).toBe('access-1')
    expect(tokenStore.getRefreshToken()).toBe('refresh-1')
    expect(localStorage.getItem('crm.refreshToken')).toBe('refresh-1')
    expect(Object.values(localStorage)).not.toContain('access-1')
  })

  it('counts a stored refresh token alone as authenticated, so a reload can restore the session', () => {
    localStorage.setItem('crm.refreshToken', 'refresh-1')
    expect(tokenStore.getAccessToken()).toBeNull()
    expect(tokenStore.isAuthenticated()).toBe(true)
  })

  it('replaces only the access token on setAccessToken', () => {
    tokenStore.setTokens('access-1', 'refresh-1')
    tokenStore.setAccessToken('access-2')

    expect(tokenStore.getAccessToken()).toBe('access-2')
    expect(tokenStore.getRefreshToken()).toBe('refresh-1')
  })

  it('wipes both tokens on clear', () => {
    tokenStore.setTokens('access-1', 'refresh-1')
    tokenStore.clear()

    expect(tokenStore.getAccessToken()).toBeNull()
    expect(tokenStore.getRefreshToken()).toBeNull()
    expect(tokenStore.isAuthenticated()).toBe(false)
  })

  it('notifies subscribers on every change until they unsubscribe', () => {
    const listener = vi.fn()
    const unsubscribe = tokenStore.subscribe(listener)

    tokenStore.setTokens('access-1', 'refresh-1')
    tokenStore.setAccessToken('access-2')
    tokenStore.clear()
    expect(listener).toHaveBeenCalledTimes(3)

    unsubscribe()
    tokenStore.setTokens('access-3', 'refresh-3')
    expect(listener).toHaveBeenCalledTimes(3)
  })
})
