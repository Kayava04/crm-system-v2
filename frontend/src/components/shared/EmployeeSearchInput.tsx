import { useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useQuery } from '@tanstack/react-query'
import { getTeachers } from '@/features/teachers/api'
import { getStaff } from '@/features/staff/api'
import { useDebouncedValue } from '@/lib/useDebouncedValue'
import { Input } from '@/components/ui/input'

export interface EmployeePick {
  id: string
  fullName: string
  kind: 'teacher' | 'staff'
}

interface EmployeeSearchInputProps {
  value: EmployeePick | null
  onChange: (value: EmployeePick | null) => void
  placeholder?: string
}

/** Picks any paid employee - a teacher or an administrator/manager - for payroll. Teachers are
 * searched server-side (there can be many); staff is a short list already fetched in full
 * elsewhere (see StaffListPage) and is filtered here client-side the same way. */
export function EmployeeSearchInput({ value, onChange, placeholder }: EmployeeSearchInputProps) {
  const { t } = useTranslation()
  const [query, setQuery] = useState('')
  const debouncedQuery = useDebouncedValue(query)
  const active = !value && debouncedQuery.length >= 2

  const { data: teacherPage } = useQuery({
    queryKey: ['teachers', 'search-picker', debouncedQuery],
    queryFn: () => getTeachers({ search: debouncedQuery, page: 1, pageSize: 10 }),
    enabled: active,
  })

  const { data: staff } = useQuery({
    queryKey: ['staff', 'list', 'search-picker'],
    queryFn: () => getStaff(true),
    enabled: active,
    staleTime: 60_000,
  })

  const matches = useMemo((): EmployeePick[] => {
    const teacherMatches: EmployeePick[] =
      teacherPage?.items.map((t) => ({ id: t.id, fullName: t.fullName, kind: 'teacher' as const })) ?? []

    const term = debouncedQuery.trim().toLowerCase()
    const staffMatches: EmployeePick[] = (staff ?? [])
      .filter((s) => (s.fullName ?? s.email).toLowerCase().includes(term) || s.email.toLowerCase().includes(term))
      .map((s) => ({ id: s.id, fullName: s.fullName ?? s.email, kind: 'staff' as const }))

    return [...teacherMatches, ...staffMatches]
  }, [teacherPage, staff, debouncedQuery])

  if (value) {
    return (
      <div className="flex items-center justify-between rounded-md border border-input px-3 py-2 text-sm">
        <span>
          {value.fullName}{' '}
          <span className="text-xs text-muted-foreground">
            {value.kind === 'teacher' ? t('billing.payroll.kindTeacher') : t('billing.payroll.kindStaff')}
          </span>
        </span>
        <button
          type="button"
          className="text-xs text-muted-foreground hover:text-foreground"
          onClick={() => onChange(null)}
        >
          {t('common.cancel')}
        </button>
      </div>
    )
  }

  return (
    <div className="flex flex-col gap-1">
      <Input value={query} onChange={(e) => setQuery(e.target.value)} placeholder={placeholder} />
      {matches.length > 0 && (
        <div className="flex flex-col rounded-md border border-border">
          {matches.map((pick) => (
            <button
              key={`${pick.kind}-${pick.id}`}
              type="button"
              className="flex items-center justify-between px-3 py-1.5 text-left text-sm hover:bg-muted"
              onClick={() => {
                onChange(pick)
                setQuery('')
              }}
            >
              <span>{pick.fullName}</span>
              <span className="text-xs text-muted-foreground">
                {pick.kind === 'teacher' ? t('billing.payroll.kindTeacher') : t('billing.payroll.kindStaff')}
              </span>
            </button>
          ))}
        </div>
      )}
    </div>
  )
}
