import { describe, expect, it, vi, beforeEach, type Mock } from 'vitest'
import { render, screen } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { useAuth } from '@/features/auth/AuthProvider'
import { RequireAuth } from './RequireAuth'
import type { MeResponse } from '@/api/types'

vi.mock('@/features/auth/AuthProvider', () => ({
  useAuth: vi.fn(),
}))

function mockUser(overrides: Partial<MeResponse> = {}): MeResponse {
  return {
    userId: 'u1',
    email: 'x@example.com',
    mustChangePassword: false,
    roles: ['Student'],
    permissions: [],
    profile: null,
    contact: { firstName: null, lastName: null, fullName: null, phoneNumber: null },
    hasPhoto: false,
    photoUrl: null,
    ...overrides,
  }
}

function renderAt(
  authValue: { status: 'loading' | 'authenticated' | 'unauthenticated'; user: MeResponse | null },
  allowMustChange = false,
) {
  ;(useAuth as unknown as Mock).mockReturnValue(authValue)
  return render(
    <MemoryRouter initialEntries={['/dashboard']}>
      <Routes>
        <Route
          path="/dashboard"
          element={
            <RequireAuth allowMustChange={allowMustChange}>{'DASHBOARD CONTENT'}</RequireAuth>
          }
        />
        <Route path="/login" element={<>{'LOGIN PAGE'}</>} />
        <Route path="/change-password" element={<>{'CHANGE PASSWORD PAGE'}</>} />
        <Route path="/" element={<>{'HOME PAGE'}</>} />
      </Routes>
    </MemoryRouter>,
  )
}

beforeEach(() => {
  vi.clearAllMocks()
})

describe('RequireAuth', () => {
  it('shows a full-page spinner while auth status is loading', () => {
    renderAt({ status: 'loading', user: null })
    expect(screen.getByRole('status')).toBeInTheDocument()
    expect(screen.queryByText('DASHBOARD CONTENT')).not.toBeInTheDocument()
  })

  it('redirects to /login when unauthenticated', () => {
    renderAt({ status: 'unauthenticated', user: null })
    expect(screen.getByText('LOGIN PAGE')).toBeInTheDocument()
  })

  it('renders children for a normally authenticated user', () => {
    renderAt({ status: 'authenticated', user: mockUser({ mustChangePassword: false }) })
    expect(screen.getByText('DASHBOARD CONTENT')).toBeInTheDocument()
  })

  it('forces an unchanged temporary password to /change-password', () => {
    renderAt({ status: 'authenticated', user: mockUser({ mustChangePassword: true }) })
    expect(screen.getByText('CHANGE PASSWORD PAGE')).toBeInTheDocument()
    expect(screen.queryByText('DASHBOARD CONTENT')).not.toBeInTheDocument()
  })

  it('lets a mustChangePassword user reach the change-password route itself', () => {
    renderAt({ status: 'authenticated', user: mockUser({ mustChangePassword: true }) }, true)
    expect(screen.getByText('DASHBOARD CONTENT')).toBeInTheDocument()
  })

  it('bounces a user who already changed their password away from the change-password screen', () => {
    renderAt({ status: 'authenticated', user: mockUser({ mustChangePassword: false }) }, true)
    expect(screen.getByText('HOME PAGE')).toBeInTheDocument()
  })
})
