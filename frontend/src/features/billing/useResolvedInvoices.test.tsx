import { describe, expect, it, vi, beforeEach, type Mock } from 'vitest'
import { renderHook, waitFor } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import type { ReactNode } from 'react'
import type { InvoiceListItem } from './api'
import { getStudentById } from '@/features/students/api'
import { getEnrollmentById } from '@/features/enrollments/api'
import { getAllCoursesForLookup } from '@/features/courses/api'
import { useResolvedInvoices } from './useResolvedInvoices'

vi.mock('@/features/students/api', () => ({ getStudentById: vi.fn() }))
vi.mock('@/features/enrollments/api', () => ({ getEnrollmentById: vi.fn() }))
vi.mock('@/features/courses/api', () => ({ getAllCoursesForLookup: vi.fn() }))

function invoice(id: string, studentId: string, enrollmentId: string) {
  return { id, studentId, enrollmentId } as InvoiceListItem
}

function renderResolved(list: InvoiceListItem[]) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  const wrapper = ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  )
  return renderHook(() => useResolvedInvoices(list), { wrapper })
}

beforeEach(() => {
  vi.clearAllMocks()
  ;(getStudentById as Mock).mockImplementation(async (id: string) => ({
    id,
    firstName: 'Olena',
    lastName: `Koval-${id}`,
  }))
  ;(getEnrollmentById as Mock).mockImplementation(async (id: string) => ({
    id,
    courseId: id === 'e1' ? 'c1' : 'c2',
  }))
  ;(getAllCoursesForLookup as Mock).mockResolvedValue([
    { id: 'c1', name: 'English B1' },
    { id: 'c2', name: 'German A2' },
  ])
})

describe('useResolvedInvoices', () => {
  it('resolves the student name and the course through the enrollment', async () => {
    const list = [invoice('i1', 's1', 'e1'), invoice('i2', 's2', 'e2')]
    const { result } = renderResolved(list)

    await waitFor(() =>
      expect(result.current.every((r) => r.studentName && r.courseName)).toBe(true),
    )

    expect(result.current).toEqual([
      { row: list[0], studentName: 'Koval-s1 Olena', courseId: 'c1', courseName: 'English B1' },
      { row: list[1], studentName: 'Koval-s2 Olena', courseId: 'c2', courseName: 'German A2' },
    ])
  })

  it('fetches each student and enrollment once across repeated invoices', async () => {
    const { result } = renderResolved([
      invoice('i1', 's1', 'e1'),
      invoice('i2', 's1', 'e1'),
      invoice('i3', 's1', 'e1'),
    ])

    await waitFor(() => expect(result.current.every((r) => r.courseName)).toBe(true))

    expect(getStudentById).toHaveBeenCalledTimes(1)
    expect(getEnrollmentById).toHaveBeenCalledTimes(1)
  })

  it('still shows the student when the enrollment cannot be loaded', async () => {
    ;(getEnrollmentById as Mock).mockRejectedValue(new Error('404'))
    const { result } = renderResolved([invoice('i1', 's1', 'e1')])

    await waitFor(() => expect(result.current[0].studentName).toBe('Koval-s1 Olena'))

    expect(result.current[0].courseId).toBeUndefined()
    expect(result.current[0].courseName).toBeUndefined()
  })
})
