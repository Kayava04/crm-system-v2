import { describe, expect, it, vi, beforeEach, type Mock } from 'vitest'
import { renderHook } from '@testing-library/react'
import { useAuth } from './AuthProvider'
import { useCan, useCanAny, useHasRole, useIsStaff } from './useCan'
import type { MeResponse } from '@/api/types'

vi.mock('./AuthProvider', () => ({
  useAuth: vi.fn(),
}))

function mockUser(overrides: Partial<MeResponse> = {}): MeResponse {
  return {
    userId: 'u1',
    email: 'teacher@example.com',
    mustChangePassword: false,
    roles: ['Teacher'],
    permissions: [],
    profile: null,
    contact: { firstName: null, lastName: null, fullName: null, phoneNumber: null },
    hasPhoto: false,
    photoUrl: null,
    ...overrides,
  }
}

function setUser(user: MeResponse | null) {
  ;(useAuth as unknown as Mock).mockReturnValue({ user })
}

beforeEach(() => {
  vi.clearAllMocks()
})

describe('useCan', () => {
  it('is false with no user at all', () => {
    setUser(null)
    const { result } = renderHook(() => useCan('CanViewStudents'))
    expect(result.current).toBe(false)
  })

  it('is false when the user holds no permissions (Teacher/Student by design)', () => {
    setUser(mockUser({ roles: ['Teacher'], permissions: [] }))
    const { result } = renderHook(() => useCan('CanViewStudents'))
    expect(result.current).toBe(false)
  })

  it('is true only for a permission the Admin user actually holds', () => {
    setUser(mockUser({ roles: ['Admin'], permissions: ['CanViewStudents', 'CanManageCourses'] }))
    expect(renderHook(() => useCan('CanViewStudents')).result.current).toBe(true)
    expect(renderHook(() => useCan('CanDeleteStudents')).result.current).toBe(false)
  })
})

describe('useCanAny', () => {
  it('is false with no user', () => {
    setUser(null)
    const { result } = renderHook(() => useCanAny(['CanViewStudents', 'CanViewTeachers']))
    expect(result.current).toBe(false)
  })

  it('is true if the user holds at least one of the listed permissions', () => {
    setUser(mockUser({ roles: ['Admin'], permissions: ['CanViewTeachers'] }))
    const { result } = renderHook(() => useCanAny(['CanViewStudents', 'CanViewTeachers']))
    expect(result.current).toBe(true)
  })

  it('is false if the user holds none of the listed permissions', () => {
    setUser(mockUser({ roles: ['Admin'], permissions: ['CanManageCourses'] }))
    const { result } = renderHook(() => useCanAny(['CanViewStudents', 'CanViewTeachers']))
    expect(result.current).toBe(false)
  })
})

describe('useHasRole', () => {
  it('checks role membership regardless of permissions', () => {
    setUser(mockUser({ roles: ['Student'], permissions: [] }))
    expect(renderHook(() => useHasRole('Student')).result.current).toBe(true)
    expect(renderHook(() => useHasRole('Teacher')).result.current).toBe(false)
  })

  it('is false with no user', () => {
    setUser(null)
    expect(renderHook(() => useHasRole('Student')).result.current).toBe(false)
  })
})

describe('useIsStaff', () => {
  it('is true for Admin', () => {
    setUser(mockUser({ roles: ['Admin'] }))
    expect(renderHook(() => useIsStaff()).result.current).toBe(true)
  })

  it('is true for SuperAdmin', () => {
    setUser(mockUser({ roles: ['SuperAdmin'] }))
    expect(renderHook(() => useIsStaff()).result.current).toBe(true)
  })

  it('is false for Teacher and Student', () => {
    setUser(mockUser({ roles: ['Teacher'] }))
    expect(renderHook(() => useIsStaff()).result.current).toBe(false)
    setUser(mockUser({ roles: ['Student'] }))
    expect(renderHook(() => useIsStaff()).result.current).toBe(false)
  })
})
