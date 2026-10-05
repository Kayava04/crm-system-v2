import { useQueries, useQuery } from '@tanstack/react-query'
import { getGroups, getGroupById } from '@/features/studyGroups/api'
import { getEnrollments } from '@/features/enrollments/api'
import { getSchedules } from '@/features/scheduling/api'
import { getTeacherById } from '@/features/teachers/api'

export interface CourseTeacher {
  id: string
  name: string
}

export function useCourseTeachers(courseId: string) {
  const groupsQuery = useQuery({
    queryKey: ['study-groups', { courseId, forTeacherLookup: true }],
    queryFn: () => getGroups({ courseId, pageSize: 100 }),
    enabled: !!courseId,
  })
  const groups = groupsQuery.data?.items ?? []

  const groupDetailQueries = useQueries({
    queries: groups.map((g) => ({
      queryKey: ['study-groups', g.id, 'membership-lookup'],
      queryFn: () => getGroupById(g.id),
      staleTime: 60_000,
      retry: false,
    })),
  })
  const groupedEnrollmentIds = new Set<string>()
  for (const q of groupDetailQueries) {
    for (const m of q.data?.members ?? []) groupedEnrollmentIds.add(m.enrollmentId)
  }

  const enrollmentsQuery = useQuery({
    queryKey: ['enrollments', { courseId, forTeacherLookup: true }],
    queryFn: () => getEnrollments({ courseId, pageSize: 200 }),
    enabled: !!courseId,
  })
  const individualEnrollmentIds = (enrollmentsQuery.data?.items ?? [])
    .map((e) => e.id)
    .filter((id) => !groupedEnrollmentIds.has(id))

  const individualScheduleQueries = useQueries({
    queries: individualEnrollmentIds.map((id) => ({
      queryKey: ['schedules', { enrollmentId: id, forTeacherLookup: true }],
      queryFn: () => getSchedules({ enrollmentId: id, pageSize: 50 }),
      staleTime: 30_000,
      retry: false,
    })),
  })

  const teacherIdSet = new Set<string>()
  for (const g of groups) teacherIdSet.add(g.teacherId)
  for (const q of individualScheduleQueries) {
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

  const teachers: CourseTeacher[] = teacherQueries
    .filter((q) => !!q.data)
    .map((q) => ({ id: q.data!.id, name: `${q.data!.lastName} ${q.data!.firstName}` }))
    .sort((a, b) => a.name.localeCompare(b.name))

  const isLoading =
    groupsQuery.isPending ||
    enrollmentsQuery.isPending ||
    groupDetailQueries.some((q) => q.isPending) ||
    individualScheduleQueries.some((q) => q.isPending) ||
    teacherQueries.some((q) => q.isPending)

  return { teachers, isLoading }
}
