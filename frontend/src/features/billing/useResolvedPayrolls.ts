import { useQueries } from '@tanstack/react-query'
import type { PayrollListItem } from './api'
import { getTeacherById } from '@/features/teachers/api'

export interface ResolvedPayrollRow {
  row: PayrollListItem
  teacherName?: string
}

/** `GET /api/billing/payrolls` rows carry a raw teacherId but no display name. */
export function useResolvedPayrolls(rows: PayrollListItem[]): ResolvedPayrollRow[] {
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

  return rows.map((row) => ({ row, teacherName: teacherNameById.get(row.teacherId) }))
}
