import { describe, expect, it, vi, beforeEach, type Mock } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { useAuth } from '@/features/auth/useAuth'
import { SidebarNav } from './Sidebar'
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

function renderNav(user: MeResponse | null, path = '/', onNavigate?: () => void) {
  ;(useAuth as unknown as Mock).mockReturnValue({ user })
  return render(
    <MemoryRouter initialEntries={[path]}>
      <SidebarNav onNavigate={onNavigate} />
    </MemoryRouter>,
  )
}

const hrefs = () => screen.queryAllByRole('link').map((a) => a.getAttribute('href'))

beforeEach(() => {
  vi.clearAllMocks()
})

describe('SidebarNav', () => {
  it('shows no links without a user', () => {
    renderNav(null)
    expect(hrefs()).toEqual([])
  })

  it('shows a student only their own pages', () => {
    renderNav(mockUser({ roles: ['Student'], permissions: ['CanViewMaterials'] }))
    expect(hrefs()).toEqual(['/calendar', '/enrollments', '/invoices', '/materials'])
  })

  it('shows an admin the pages their permissions allow', () => {
    renderNav(
      mockUser({
        roles: ['Admin'],
        permissions: ['CanViewReports', 'CanViewStudents', 'CanViewCourses', 'CanManageAdmins'],
      }),
    )
    expect(hrefs()).toEqual(['/dashboard', '/calendar', '/students', '/courses', '/staff'])
  })

  it('marks the current page as active', () => {
    renderNav(mockUser({ roles: ['Admin'], permissions: ['CanViewStudents'] }), '/students/42')
    expect(screen.getByRole('link', { name: /students/i })).toHaveAttribute('aria-current', 'page')
  })

  it('calls onNavigate when a link is clicked', async () => {
    const user = userEvent.setup()
    const onNavigate = vi.fn()
    renderNav(mockUser({ roles: ['Teacher'] }), '/', onNavigate)

    await user.click(screen.getAllByRole('link')[0])

    expect(onNavigate).toHaveBeenCalledTimes(1)
  })
})
