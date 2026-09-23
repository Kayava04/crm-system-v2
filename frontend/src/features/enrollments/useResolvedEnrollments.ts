import { useQueries, useQuery } from '@tanstack/react-query'
import { getEnrollmentById } from './api'
import type { EnrollmentListItem } from './api'
import { getStudentById } from '@/features/students/api'
import { getAllCoursesForLookup } from '@/features/courses/api'

export interface ResolvedEnrollmentRow {
  row: EnrollmentListItem
  studentId?: string
  courseId?: string
  studentName?: string
  courseName?: string
  isResolving: boolean
}

/**
 * The backend's enrollment list endpoint (`GET /api/enrollments`) returns only
 * `{ id, enrollmentNumber, startDate, endDate, effectivePrice, status }` — no
 * studentId/courseId/studentName/courseName, even though the endpoint accepts
 * studentId/courseId filters. (The sibling "my enrollments" endpoint does resolve
 * a courseName, so this looks like an oversight in the admin list rather than an
 * intentional design — reported to the user rather than silently patched here.)
 *
 * To render a usable table we resolve each row's detail (which does carry both
 * ids) and then look up display names: the full course list once (small, cached),
 * and each distinct student by id (bounded by the current page size).
 */
export function useResolvedEnrollments(rows: EnrollmentListItem[]): ResolvedEnrollmentRow[] {
  const detailQueries = useQueries({
    queries: rows.map((row) => ({
      queryKey: ['enrollments', row.id, 'detail-lookup'],
      queryFn: () => getEnrollmentById(row.id),
      staleTime: 60_000,
      retry: false,
    })),
  })

  const coursesQuery = useQuery({
    queryKey: ['courses', 'lookup-all'],
    queryFn: getAllCoursesForLookup,
    staleTime: 5 * 60_000,
  })
  const courseNameById = new Map((coursesQuery.data ?? []).map((c) => [c.id, c.name]))

  const studentIds = Array.from(
    new Set(detailQueries.map((q) => q.data?.studentId).filter((id): id is string => !!id)),
  )
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

  return rows.map((row, i) => {
    const detail = detailQueries[i]?.data
    return {
      row,
      studentId: detail?.studentId,
      courseId: detail?.courseId,
      studentName: detail?.studentId ? studentNameById.get(detail.studentId) : undefined,
      courseName: detail?.courseId ? courseNameById.get(detail.courseId) : undefined,
      isResolving: !detail,
    }
  })
}
