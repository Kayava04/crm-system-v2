import { describe, expect, it, vi, beforeEach, type Mock } from 'vitest'
import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { useAuth } from '@/features/auth/useAuth'
import { HomePage } from './HomePage'
import type { MeResponse } from '@/api/types'

vi.mock('@/features/auth/useAuth', () => ({
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

function renderHome(user: MeResponse) {
  ;(useAuth as unknown as Mock).mockReturnValue({ user })
  return render(
    <MemoryRouter>
      <HomePage />
    </MemoryRouter>,
  )
}

const hrefs = () => screen.getAllByRole('link').map((a) => a.getAttribute('href'))

beforeEach(() => {
  vi.clearAllMocks()
})

describe('HomePage quick links', () => {
  it('shows a teacher the courses link, in sidebar order, when they can view courses', () => {
    renderHome(mockUser({ roles: ['Teacher'], permissions: ['CanViewCourses'] }))
    expect(hrefs()).toEqual(['/calendar', '/my-students', '/courses', '/payroll', '/materials'])
  })

  it("hides the courses link from a teacher who can't view courses", () => {
    renderHome(mockUser({ roles: ['Teacher'], permissions: [] }))
    expect(hrefs()).toEqual(['/calendar', '/my-students', '/payroll', '/materials'])
  })

  it('leaves the student links unchanged', () => {
    renderHome(mockUser({ roles: ['Student'], permissions: ['CanViewCourses'] }))
    expect(hrefs()).toEqual(['/calendar', '/enrollments', '/invoices', '/materials'])
  })
})
