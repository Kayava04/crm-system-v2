import { useQueries, useQuery } from '@tanstack/react-query'
import type { InvoiceListItem } from './api'
import { getStudentById } from '@/features/students/api'
import { getEnrollmentById } from '@/features/enrollments/api'
import { getAllCoursesForLookup } from '@/features/courses/api'

export interface ResolvedInvoiceRow {
  row: InvoiceListItem
  studentName?: string
  courseName?: string
}

/**
 * `GET /api/billing/invoices` rows carry raw studentId/enrollmentId but no
 * display names, so — same as enrollments/schedules — we resolve each row
 * individually (bounded to the current page).
 */
export function useResolvedInvoices(rows: InvoiceListItem[]): ResolvedInvoiceRow[] {
  const studentIds = Array.from(new Set(rows.map((r) => r.studentId).filter(Boolean)))
  const studentQueries = useQueries({
    queries: studentIds.map((id) => ({
      queryKey: ['students', id, 'lookup-name'],
      queryFn: () => getStudentById(id),
      staleTime: 60_000,
      retry: false,
    })),
  })
  const studentNameById = new Map(
    studentQueries
      .filter((q) => !!q.data)
      .map((q) => [q.data!.id, `${q.data!.lastName} ${q.data!.firstName}`]),
  )

  const enrollmentIds = Array.from(new Set(rows.map((r) => r.enrollmentId).filter(Boolean)))
  const enrollmentQueries = useQueries({
    queries: enrollmentIds.map((id) => ({
      queryKey: ['enrollments', id, 'detail-lookup'],
      queryFn: () => getEnrollmentById(id),
      staleTime: 60_000,
      retry: false,
    })),
  })
  const courseIdByEnrollmentId = new Map(
    enrollmentQueries.filter((q) => !!q.data).map((q) => [q.data!.id, q.data!.courseId]),
  )

  const coursesQuery = useQuery({
    queryKey: ['courses', 'lookup-all'],
    queryFn: getAllCoursesForLookup,
    staleTime: 5 * 60_000,
  })
  const courseNameById = new Map((coursesQuery.data ?? []).map((c) => [c.id, c.name]))

  return rows.map((row) => {
    const courseId = courseIdByEnrollmentId.get(row.enrollmentId)
    return {
      row,
      studentName: studentNameById.get(row.studentId),
      courseName: courseId ? courseNameById.get(courseId) : undefined,
    }
  })
}
