import { describe, expect, it, vi, beforeEach, type Mock } from 'vitest'
import { act, renderHook, waitFor } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import type { ReactNode } from 'react'
import { tokenStore } from '@/api/tokenStore'
import { changePassword, fetchMe, login } from './api'
import { AuthProvider } from './AuthProvider'
import { useAuth } from './useAuth'
import type { MeResponse } from '@/api/types'

vi.mock('./api', () => ({
  fetchMe: vi.fn(),
  login: vi.fn(),
  changePassword: vi.fn(),
}))

function mockUser(overrides: Partial<MeResponse> = {}): MeResponse {
  return {
    userId: 'u1',
    email: 'admin@example.com',
    mustChangePassword: false,
    roles: ['Admin'],
    permissions: [],
    profile: null,
    contact: {
      firstName: null,
      lastName: null,
      middleName: null,
      fullName: null,
      phoneNumber: null,
      dateOfBirth: null,
      city: null,
      country: null,
      salary: null,
    },
    hasPhoto: false,
    photoUrl: null,
    ...overrides,
  }
}

function renderAuth() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  const wrapper = ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>{children}</AuthProvider>
    </QueryClientProvider>
  )
  return renderHook(() => useAuth(), { wrapper })
}

beforeEach(() => {
  vi.clearAllMocks()
  tokenStore.clear()
})

describe('AuthProvider', () => {
  it('is unauthenticated without a refresh token and does not call the API', () => {
    const { result } = renderAuth()
    expect(result.current.status).toBe('unauthenticated')
    expect(result.current.user).toBeNull()
    expect(fetchMe).not.toHaveBeenCalled()
  })

  it('restores the session from a stored refresh token', async () => {
    localStorage.setItem('crm.refreshToken', 'refresh-1')
    ;(fetchMe as Mock).mockResolvedValue(mockUser())

    const { result } = renderAuth()

    expect(result.current.status).toBe('loading')
    await waitFor(() => expect(result.current.status).toBe('authenticated'))
    expect(result.current.user?.email).toBe('admin@example.com')
  })

  it('is unauthenticated when the stored session can no longer be loaded', async () => {
    localStorage.setItem('crm.refreshToken', 'refresh-1')
    ;(fetchMe as Mock).mockRejectedValue(new Error('401'))

    const { result } = renderAuth()

    await waitFor(() => expect(result.current.status).toBe('unauthenticated'))
    expect(result.current.user).toBeNull()
  })

  it('stores the tokens and loads the user on login', async () => {
    ;(login as Mock).mockResolvedValue({ accessToken: 'access-1', refreshToken: 'refresh-1' })
    ;(fetchMe as Mock).mockResolvedValue(mockUser())
    const { result } = renderAuth()

    let me: MeResponse | undefined
    await act(async () => {
      me = await result.current.login('admin@example.com', 'secret')
    })

    expect(login).toHaveBeenCalledWith('admin@example.com', 'secret')
    expect(tokenStore.getAccessToken()).toBe('access-1')
    expect(tokenStore.getRefreshToken()).toBe('refresh-1')
    expect(me?.email).toBe('admin@example.com')
    await waitFor(() => expect(result.current.status).toBe('authenticated'))
  })

  it('stores nothing when the login is rejected', async () => {
    ;(login as Mock).mockRejectedValue(new Error('Invalid credentials'))
    const { result } = renderAuth()

    await act(async () => {
      await expect(result.current.login('admin@example.com', 'wrong')).rejects.toThrow()
    })

    expect(tokenStore.isAuthenticated()).toBe(false)
    expect(fetchMe).not.toHaveBeenCalled()
  })

  it('clears the tokens and the user on logout', async () => {
    localStorage.setItem('crm.refreshToken', 'refresh-1')
    ;(fetchMe as Mock).mockResolvedValue(mockUser())
    const { result } = renderAuth()
    await waitFor(() => expect(result.current.status).toBe('authenticated'))

    act(() => result.current.logout())

    expect(tokenStore.isAuthenticated()).toBe(false)
    await waitFor(() => expect(result.current.status).toBe('unauthenticated'))
    expect(result.current.user).toBeNull()
  })

  it('reloads the user after a password change', async () => {
    localStorage.setItem('crm.refreshToken', 'refresh-1')
    ;(fetchMe as Mock)
      .mockResolvedValueOnce(mockUser({ mustChangePassword: true }))
      .mockResolvedValueOnce(mockUser({ mustChangePassword: false }))
    ;(changePassword as Mock).mockResolvedValue(undefined)
    const { result } = renderAuth()
    await waitFor(() => expect(result.current.user?.mustChangePassword).toBe(true))

    const input = {
      oldPassword: 'old',
      newPassword: 'new-password',
      confirmNewPassword: 'new-password',
    }
    await act(async () => {
      await result.current.changePassword(input)
    })

    expect(changePassword).toHaveBeenCalledWith(input)
    await waitFor(() => expect(result.current.user?.mustChangePassword).toBe(false))
  })
})

describe('useAuth', () => {
  it('throws outside AuthProvider', () => {
    const swallow = (event: ErrorEvent) => event.preventDefault()
    window.addEventListener('error', swallow)
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {})

    expect(() => renderHook(() => useAuth())).toThrow('useAuth must be used within AuthProvider')

    consoleError.mockRestore()
    window.removeEventListener('error', swallow)
  })
})
