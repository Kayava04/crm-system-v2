import { useQueries, useQuery } from '@tanstack/react-query'
import { getSchedules } from './api'
import type { ScheduleListItem } from './api'
import { getEnrollments, getEnrollmentById } from '@/features/enrollments/api'
import { getAllGroupsForLookup, getGroupById } from '@/features/studyGroups/api'
import { getTeacherById } from '@/features/teachers/api'
import { getAllCoursesForLookup } from '@/features/courses/api'

export interface StudentLessonRow {
  row: ScheduleListItem
  teacherName?: string
  courseName?: string
  groupName?: string
}

/**
 * There is no `GET /api/schedules?studentId=` filter on the backend — a
 * schedule row carries either an enrollmentId (individual lesson) or a
 * groupId (group lesson), never a studentId directly (confirmed against
 * Scheduling.Domain.Entities.Schedule, which models this as "exactly one of
 * EnrollmentId/GroupId is set"). So a student's lesson history is assembled
 * from two sources: (1) the individual schedules of their own enrollments,
 * and (2) the schedules of any study group one of those enrollments has
 * been added to as a member. This composes several endpoints because the
 * backend has no single "lessons for this student" endpoint — a real gap,
 * not a frontend shortcut, and one worth raising with backend if a proper
 * filter is ever added there.
 */
export function useStudentLessons(studentId: string) {
  const enrollmentsQuery = useQuery({
    queryKey: ['enrollments', { studentId, forLessons: true }],
    queryFn: () => getEnrollments({ studentId, pageSize: 50 }),
    enabled: !!studentId,
  })
  const enrollmentIds = (enrollmentsQuery.data?.items ?? []).map((e) => e.id)

  const enrollmentDetailQueries = useQueries({
    queries: enrollmentIds.map((id) => ({
      queryKey: ['enrollments', id, 'detail-lookup'],
      queryFn: () => getEnrollmentById(id),
      staleTime: 60_000,
      retry: false,
    })),
  })
  const courseIdByEnrollmentId = new Map(
    enrollmentDetailQueries.filter((q) => !!q.data).map((q) => [q.data!.id, q.data!.courseId]),
  )
  const enrollmentCourseIds = new Set(courseIdByEnrollmentId.values())

  const individualScheduleQueries = useQueries({
    queries: enrollmentIds.map((id) => ({
      queryKey: ['schedules', { enrollmentId: id, forLessons: true }],
      queryFn: () => getSchedules({ enrollmentId: id, pageSize: 100 }),
      staleTime: 30_000,
      retry: false,
    })),
  })

  const groupsQuery = useQuery({
    queryKey: ['study-groups', 'lookup-all'],
    queryFn: getAllGroupsForLookup,
    staleTime: 5 * 60_000,
  })
  const candidateGroups = (groupsQuery.data ?? []).filter((g) =>
    enrollmentCourseIds.has(g.courseId),
  )

  const groupDetailQueries = useQueries({
    queries: candidateGroups.map((g) => ({
      queryKey: ['study-groups', g.id, 'membership-lookup'],
      queryFn: () => getGroupById(g.id),
      staleTime: 60_000,
      retry: false,
    })),
  })
  const enrollmentIdSet = new Set(enrollmentIds)
  const matchedGroups = groupDetailQueries
    .filter((q) => !!q.data)
    .map((q) => q.data!)
    .filter((g) => g.members.some((m) => enrollmentIdSet.has(m.enrollmentId)))

  const groupScheduleQueries = useQueries({
    queries: matchedGroups.map((g) => ({
      queryKey: ['schedules', { groupId: g.id, forLessons: true }],
      queryFn: () => getSchedules({ groupId: g.id, pageSize: 100 }),
      staleTime: 30_000,
      retry: false,
    })),
  })

  const teacherIdSet = new Set<string>()
  for (const q of individualScheduleQueries) {
    for (const r of q.data?.items ?? []) teacherIdSet.add(r.teacherId)
  }
  for (const q of groupScheduleQueries) {
    for (const r of q.data?.items ?? []) teacherIdSet.add(r.teacherId)
  }
  const teacherIds = Array.from(teacherIdSet)

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
  const groupNameById = new Map(matchedGroups.map((g) => [g.id, g.name]))

  const rows: StudentLessonRow[] = []
  const seen = new Set<string>()
  for (const q of individualScheduleQueries) {
    for (const r of q.data?.items ?? []) {
      if (seen.has(r.id)) continue
      seen.add(r.id)
      const courseId = r.enrollmentId ? courseIdByEnrollmentId.get(r.enrollmentId) : undefined
      rows.push({
        row: r,
        teacherName: teacherNameById.get(r.teacherId),
        courseName: courseId ? courseNameById.get(courseId) : undefined,
      })
    }
  }
  for (const q of groupScheduleQueries) {
    for (const r of q.data?.items ?? []) {
      if (seen.has(r.id)) continue
      seen.add(r.id)
      const group = r.groupId ? matchedGroups.find((g) => g.id === r.groupId) : undefined
      rows.push({
        row: r,
        teacherName: teacherNameById.get(r.teacherId),
        courseName: group ? courseNameById.get(group.courseId) : undefined,
        groupName: r.groupId ? groupNameById.get(r.groupId) : undefined,
      })
    }
  }
  // Ascending (earliest first), matching the backend's own default
  // order for GET /api/schedules (ScheduleRepository's filtered
  // GetAllAsync orders by ScheduledDate ascending) — the same order the
  // teacher's Lessons tab shows, since it reads that endpoint directly.
  rows.sort((a, b) => a.row.scheduledDate.localeCompare(b.row.scheduledDate))

  const isLoading =
    enrollmentsQuery.isPending ||
    individualScheduleQueries.some((q) => q.isPending) ||
    groupDetailQueries.some((q) => q.isPending) ||
    groupScheduleQueries.some((q) => q.isPending)

  return { rows, isLoading }
}
