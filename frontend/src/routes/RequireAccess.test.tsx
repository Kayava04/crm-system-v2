import { describe, expect, it, vi, beforeEach, type Mock } from 'vitest'
import { render, screen } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { useAuth } from '@/features/auth/AuthProvider'
import { RequireAccess } from './RequireAccess'
import type { MeResponse } from '@/api/types'

vi.mock('@/features/auth/AuthProvider', () => ({
  useAuth: vi.fn(),
}))

function mockUser(overrides: Partial<MeResponse> = {}): MeResponse {
  return {
    userId: 'u1',
    email: 'x@example.com',
    mustChangePassword: false,
    roles: [],
    permissions: [],
    profile: null,
    contact: { firstName: null, lastName: null, fullName: null, phoneNumber: null },
    hasPhoto: false,
    photoUrl: null,
    ...overrides,
  }
}

function renderGuarded(
  user: MeResponse | null,
  props: Omit<Parameters<typeof RequireAccess>[0], 'children'>,
) {
  ;(useAuth as unknown as Mock).mockReturnValue({ user })
  return render(
    <MemoryRouter initialEntries={['/protected']}>
      <Routes>
        <Route
          path="/protected"
          element={<RequireAccess {...props}>{'PROTECTED CONTENT'}</RequireAccess>}
        />
        <Route path="/403" element={<>{'FORBIDDEN PAGE'}</>} />
      </Routes>
    </MemoryRouter>,
  )
}

beforeEach(() => {
  vi.clearAllMocks()
})

describe('RequireAccess', () => {
  it('renders children when no permission or roles are required (open route)', () => {
    renderGuarded(mockUser({ roles: [], permissions: [] }), {})
    expect(screen.getByText('PROTECTED CONTENT')).toBeInTheDocument()
  })

  it('allows access via permission alone', () => {
    renderGuarded(mockUser({ permissions: ['CanViewMaterials'] }), {
      permission: 'CanViewMaterials',
    })
    expect(screen.getByText('PROTECTED CONTENT')).toBeInTheDocument()
  })

  it('redirects to /403 when the user holds neither the permission nor a listed role', () => {
    renderGuarded(mockUser({ roles: ['Student'], permissions: [] }), {
      permission: 'CanViewMaterials',
    })
    expect(screen.getByText('FORBIDDEN PAGE')).toBeInTheDocument()
    expect(screen.queryByText('PROTECTED CONTENT')).not.toBeInTheDocument()
  })

  // Regression test: a Teacher/Student route gated on permission alone used to be
  // unreachable for Teacher/Student, who hold zero permissions by design (self-caught
  // bug in the Materials slice). permission + roles must use OR semantics.
  it('allows access via a listed role even without the permission (OR semantics)', () => {
    renderGuarded(mockUser({ roles: ['Teacher'], permissions: [] }), {
      permission: 'CanManageMaterials',
      roles: ['Teacher', 'Student'],
    })
    expect(screen.getByText('PROTECTED CONTENT')).toBeInTheDocument()
  })

  it('redirects to /403 when the user has neither the permission nor any listed role', () => {
    renderGuarded(mockUser({ roles: ['Student'], permissions: [] }), {
      permission: 'CanManageMaterials',
      roles: ['Teacher'],
    })
    expect(screen.getByText('FORBIDDEN PAGE')).toBeInTheDocument()
  })

  it('allows access via permission even when a roles list is also present but not matched', () => {
    renderGuarded(mockUser({ roles: ['Admin'], permissions: ['CanManageMaterials'] }), {
      permission: 'CanManageMaterials',
      roles: ['Teacher', 'Student'],
    })
    expect(screen.getByText('PROTECTED CONTENT')).toBeInTheDocument()
  })
})
