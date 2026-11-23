import { describe, expect, it, vi, beforeEach, type Mock } from 'vitest'
import { renderHook, waitFor } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import type { ReactNode } from 'react'
import { getEnrollmentById, type EnrollmentListItem } from './api'
import { getStudentById } from '@/features/students/api'
import { getAllCoursesForLookup } from '@/features/courses/api'
import { useResolvedEnrollments } from './useResolvedEnrollments'

vi.mock('./api', () => ({ getEnrollmentById: vi.fn() }))
vi.mock('@/features/students/api', () => ({ getStudentById: vi.fn() }))
vi.mock('@/features/courses/api', () => ({ getAllCoursesForLookup: vi.fn() }))

const enrollments: Record<string, { id: string; studentId: string; courseId: string }> = {
  e1: { id: 'e1', studentId: 's1', courseId: 'c1' },
  e2: { id: 'e2', studentId: 's1', courseId: 'c2' },
  e3: { id: 'e3', studentId: 's2', courseId: 'c1' },
}

const students: Record<string, { id: string; firstName: string; lastName: string }> = {
  s1: { id: 's1', firstName: 'Olena', lastName: 'Koval' },
  s2: { id: 's2', firstName: 'Ivan', lastName: 'Bondar' },
}

function rows(...ids: string[]) {
  return ids.map((id) => ({ id }) as EnrollmentListItem)
}

function renderResolved(list: EnrollmentListItem[]) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  const wrapper = ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  )
  return renderHook(() => useResolvedEnrollments(list), { wrapper })
}

beforeEach(() => {
  vi.clearAllMocks()
  ;(getEnrollmentById as Mock).mockImplementation(async (id: string) => enrollments[id])
  ;(getStudentById as Mock).mockImplementation(async (id: string) => students[id])
  ;(getAllCoursesForLookup as Mock).mockResolvedValue([
    { id: 'c1', name: 'English B1' },
    { id: 'c2', name: 'German A2' },
  ])
})

describe('useResolvedEnrollments', () => {
  it('marks rows as resolving until their details arrive', () => {
    const { result } = renderResolved(rows('e1'))
    expect(result.current[0].isResolving).toBe(true)
    expect(result.current[0].studentName).toBeUndefined()
  })

  it('resolves the student and course names for each row, in order', async () => {
    const list = rows('e1', 'e2', 'e3')
    const { result } = renderResolved(list)

    await waitFor(() =>
      expect(result.current.every((r) => r.studentName && r.courseName)).toBe(true),
    )

    expect(result.current.map((r) => [r.row, r.studentName, r.courseName])).toEqual([
      [list[0], 'Koval Olena', 'English B1'],
      [list[1], 'Koval Olena', 'German A2'],
      [list[2], 'Bondar Ivan', 'English B1'],
    ])
    expect(result.current.every((r) => !r.isResolving)).toBe(true)
  })

  it('loads each student only once even when they appear on several rows', async () => {
    const { result } = renderResolved(rows('e1', 'e2', 'e3'))

    await waitFor(() => expect(result.current.every((r) => r.studentName)).toBe(true))

    expect(getStudentById).toHaveBeenCalledTimes(2)
    expect(getAllCoursesForLookup).toHaveBeenCalledTimes(1)
  })

  it('keeps the ids but leaves the name empty for an unknown course', async () => {
    ;(getAllCoursesForLookup as Mock).mockResolvedValue([])
    const { result } = renderResolved(rows('e1'))

    await waitFor(() => expect(result.current[0].studentName).toBe('Koval Olena'))

    expect(result.current[0].courseId).toBe('c1')
    expect(result.current[0].courseName).toBeUndefined()
  })

  it('returns nothing for an empty list', () => {
    const { result } = renderResolved([])
    expect(result.current).toEqual([])
    expect(getEnrollmentById).not.toHaveBeenCalled()
  })
})
