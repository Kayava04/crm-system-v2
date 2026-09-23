import { useQueries, useQuery } from '@tanstack/react-query'
import type { GroupListItem } from './api'
import { getTeacherById } from '@/features/teachers/api'
import { getAllCoursesForLookup } from '@/features/courses/api'

export interface ResolvedGroupRow {
  row: GroupListItem
  courseName?: string
  teacherName?: string
}

/**
 * `GET /api/study-groups` rows carry raw courseId/teacherId but no names, so —
 * same as enrollments/schedules — we resolve each row's teacher individually
 * (bounded to the current page) and courses via the small bounded lookup list.
 */
export function useResolvedGroups(rows: GroupListItem[]): ResolvedGroupRow[] {
  const teacherIds = Array.from(new Set(rows.map((r) => r.teacherId).filter(Boolean)))
  const teacherQueries = useQueries({
    queries: teacherIds.map((id) => ({
      queryKey: ['teachers', id, 'lookup-name'],
      queryFn: () => getTeacherById(id),
      staleTime: 60_000,
      retry: false,
    })),
  })
  const teacherNameById = new Map(
    teacherQueries
      .filter((q) => !!q.data)
      .map((q) => [q.data!.id, `${q.data!.lastName} ${q.data!.firstName}`]),
  )

  const coursesQuery = useQuery({
    queryKey: ['courses', 'lookup-all'],
    queryFn: getAllCoursesForLookup,
    staleTime: 5 * 60_000,
  })
  const courseNameById = new Map((coursesQuery.data ?? []).map((c) => [c.id, c.name]))

  return rows.map((row) => ({
    row,
    courseName: courseNameById.get(row.courseId),
    teacherName: teacherNameById.get(row.teacherId),
  }))
}
