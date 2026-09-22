import { useQueries, useQuery } from '@tanstack/react-query'
import type { PayrollListItem } from './api'
import { getTeacherById } from '@/features/teachers/api'
import { getStaff } from '@/features/staff/api'

export interface ResolvedPayrollRow {
  row: PayrollListItem
  employeeName?: string
}

/** `GET /api/billing/payrolls` rows carry a raw teacherId or userId but no display name. */
export function useResolvedPayrolls(rows: PayrollListItem[]): ResolvedPayrollRow[] {
  const teacherIds = Array.from(new Set(rows.map((r) => r.teacherId).filter((id): id is string => !!id)))
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

  const hasStaffRows = rows.some((r) => !!r.userId)
  const { data: staff } = useQuery({
    queryKey: ['staff', 'list', 'lookup-name'],
    queryFn: () => getStaff(),
    enabled: hasStaffRows,
    staleTime: 60_000,
  })
  const staffNameById = new Map((staff ?? []).map((s) => [s.id, s.fullName ?? s.email]))

  return rows.map((row) => ({
    row,
    employeeName: row.teacherId ? teacherNameById.get(row.teacherId) : staffNameById.get(row.userId ?? ''),
  }))
}
