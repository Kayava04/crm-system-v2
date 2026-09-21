import { useQueries, useQuery } from '@tanstack/react-query'
import type { ScheduleListItem } from './api'
import { getTeacherById } from '@/features/teachers/api'
import { getStudentById } from '@/features/students/api'
import { getEnrollmentById } from '@/features/enrollments/api'
import { getGroupById } from '@/features/studyGroups/api'
import { getAllCoursesForLookup } from '@/features/courses/api'

export interface ResolvedScheduleRow {
  row: ScheduleListItem
  teacherName?: string
  courseName?: string
  groupName?: string
  studentName?: string
  isResolving: boolean
}

/**
 * `GET /api/schedules` rows carry raw ids (teacherId, and either enrollmentId or
 * groupId) but no display names, so — same as enrollments — we resolve each row
 * individually (bounded to the current page) to build a readable calendar table.
 */
export function useResolvedSchedules(rows: ScheduleListItem[]): ResolvedScheduleRow[] {
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

  const enrollmentIds = Array.from(
    new Set(rows.map((r) => r.enrollmentId).filter((id): id is string => !!id)),
  )
  const enrollmentQueries = useQueries({
    queries: enrollmentIds.map((id) => ({
      queryKey: ['enrollments', id, 'detail-lookup'],
      queryFn: () => getEnrollmentById(id),
      staleTime: 60_000,
      retry: false,
    })),
  })
  const enrollmentById = new Map(
    enrollmentQueries.filter((q) => !!q.data).map((q) => [q.data!.id, q.data!]),
  )

  const studentIds = Array.from(
    new Set(
      Array.from(enrollmentById.values())
        .map((e) => e.studentId)
        .filter(Boolean),
    ),
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

  const groupIds = Array.from(
    new Set(rows.map((r) => r.groupId).filter((id): id is string => !!id)),
  )
  const groupQueries = useQueries({
    queries: groupIds.map((id) => ({
      queryKey: ['study-groups', id],
      queryFn: () => getGroupById(id),
      staleTime: 60_000,
      retry: false,
    })),
  })
  const groupById = new Map(groupQueries.filter((q) => !!q.data).map((q) => [q.data!.id, q.data!]))

  const coursesQuery = useQuery({
    queryKey: ['courses', 'lookup-all'],
    queryFn: getAllCoursesForLookup,
    staleTime: 5 * 60_000,
  })
  const courseNameById = new Map((coursesQuery.data ?? []).map((c) => [c.id, c.name]))

  return rows.map((row) => {
    const teacherName = teacherNameById.get(row.teacherId)
    if (row.enrollmentId) {
      const enrollment = enrollmentById.get(row.enrollmentId)
      return {
        row,
        teacherName,
        studentName: enrollment ? studentNameById.get(enrollment.studentId) : undefined,
        courseName: enrollment ? courseNameById.get(enrollment.courseId) : undefined,
        isResolving: !teacherName || !enrollment,
      }
    }
    if (row.groupId) {
      const group = groupById.get(row.groupId)
      return {
        row,
        teacherName,
        groupName: group?.name,
        courseName: group ? courseNameById.get(group.courseId) : undefined,
        isResolving: !teacherName || !group,
      }
    }
    return { row, teacherName, isResolving: !teacherName }
  })
}
