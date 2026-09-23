import { useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { useQuery, useQueryClient, keepPreviousData } from '@tanstack/react-query'
import { Plus } from 'lucide-react'
import { getEnrollments } from '@/features/enrollments/api'
import type { EnrollmentStatus } from '@/features/enrollments/api'
import { useResolvedEnrollments } from '@/features/enrollments/useResolvedEnrollments'
import { getAllCoursesForLookup } from '@/features/courses/api'
import { enumLabel } from '@/lib/enumLabels'
import { toNum, formatCurrency, formatDate } from '@/lib/utils'
import { useDebouncedValue } from '@/lib/useDebouncedValue'
import { useCan } from '@/features/auth/useCan'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { DataTable, type DataTableColumn } from '@/components/shared/DataTable'
import { Pagination } from '@/components/shared/Pagination'
import { CreateEnrollmentDialog } from '@/components/shared/CreateEnrollmentDialog'

const STATUSES: EnrollmentStatus[] = ['Draft', 'Active', 'Suspended', 'Completed', 'Terminated']

function statusVariant(status: string): 'success' | 'warning' | 'secondary' | 'destructive' {
  if (status === 'Active') return 'success'
  if (status === 'Suspended') return 'warning'
  if (status === 'Completed') return 'secondary'
  return 'destructive'
}

interface EnrollmentRow {
  id: string
  enrollmentNumber: string
  status: EnrollmentStatus
  startDate: string
  endDate: string
  effectivePrice: number | string
  studentName?: string
  courseName?: string
  isResolving: boolean
}

/** Staff-facing enrollments list, backed by the admin `GET /api/enrollments`
 * endpoint - unlike `MyEnrollmentsPage` (student-only, `/api/enrollments/my`),
 * this is what an Admin/staff member with CanViewEnrollments actually needs
 * when they land on `/enrollments`. See `useResolvedEnrollments` for why each
 * row is resolved individually: the list endpoint doesn't return student/course
 * names or ids, only detail lookups do. */
export function StaffEnrollmentsListPage() {
  const { t, i18n } = useTranslation()
  const lang = i18n.language === 'en' ? 'en' : 'uk'
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const canManage = useCan('CanManageEnrollments')

  const [searchParams, setSearchParams] = useSearchParams()
  const [createOpen, setCreateOpen] = useState(false)
  const [searchInput, setSearchInput] = useState('')
  const debouncedSearch = useDebouncedValue(searchInput)

  const page = Number(searchParams.get('page') ?? '1')
  const status = (searchParams.get('status') ?? '') as EnrollmentStatus | ''
  const courseId = searchParams.get('courseId') ?? ''

  const filters = useMemo(
    () => ({
      status: status || undefined,
      courseId: courseId || undefined,
      page,
      pageSize: 20,
    }),
    [status, courseId, page],
  )

  const { data, isPending, isPlaceholderData } = useQuery({
    queryKey: ['enrollments', 'list', filters],
    queryFn: () => getEnrollments(filters),
    placeholderData: keepPreviousData,
  })

  /** `GET /api/enrollments` accepts a real `courseId` filter (applied
   * server-side above) but has no free-text search param, so - same as the
   * invoices list - name search is applied client-side over the resolved
   * current page. */
  const { data: courses } = useQuery({
    queryKey: ['courses', 'lookup-all'],
    queryFn: getAllCoursesForLookup,
    staleTime: 5 * 60_000,
  })

  const resolved = useResolvedEnrollments(data?.items ?? [])
  const rows: EnrollmentRow[] = useMemo(() => {
    const term = debouncedSearch.trim().toLowerCase()
    return resolved
      .map((r) => ({
        id: r.row.id,
        enrollmentNumber: r.row.enrollmentNumber,
        status: r.row.status,
        startDate: r.row.startDate,
        endDate: r.row.endDate,
        effectivePrice: r.row.effectivePrice,
        studentName: r.studentName,
        courseName: r.courseName,
        isResolving: r.isResolving,
      }))
      .filter((row) => {
        if (!term) return true
        return (
          row.enrollmentNumber.toLowerCase().includes(term) ||
          (row.studentName ?? '').toLowerCase().includes(term) ||
          (row.courseName ?? '').toLowerCase().includes(term)
        )
      })
  }, [resolved, debouncedSearch])

  function updateParam(key: string, value: string) {
    const next = new URLSearchParams(searchParams)
    if (value) next.set(key, value)
    else next.delete(key)
    next.delete('page')
    setSearchParams(next, { replace: true })
  }

  function goToPage(nextPage: number) {
    const next = new URLSearchParams(searchParams)
    next.set('page', String(nextPage))
    setSearchParams(next, { replace: true })
  }

  const columns: DataTableColumn<EnrollmentRow>[] = [
    { key: 'number', header: t('enrollments.columns.number'), cell: (row) => row.enrollmentNumber },
    {
      key: 'student',
      header: t('enrollments.columns.student'),
      cell: (row) => row.studentName ?? (row.isResolving ? t('enrollments.detail.resolving') : '—'),
    },
    {
      key: 'course',
      header: t('enrollments.columns.course'),
      cell: (row) => row.courseName ?? (row.isResolving ? t('enrollments.detail.resolving') : '—'),
    },
    {
      key: 'dates',
      header: t('enrollments.columns.dates'),
      cell: (row) => `${formatDate(row.startDate, lang)} – ${formatDate(row.endDate, lang)}`,
    },
    {
      key: 'price',
      header: t('enrollments.columns.price'),
      cell: (row) => formatCurrency(row.effectivePrice, lang),
    },
    {
      key: 'status',
      header: t('enrollments.columns.status'),
      cell: (row) => (
        <Badge variant={statusVariant(row.status)}>
          {enumLabel(t, 'enrollmentStatus', row.status)}
        </Badge>
      ),
    },
  ]

  return (
    <div className="flex flex-col gap-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-2xl font-semibold tracking-tight">{t('nav.enrollments')}</h1>
        {canManage && (
          <Button onClick={() => setCreateOpen(true)}>
            <Plus />
            {t('enrollments.createDialog.title')}
          </Button>
        )}
      </div>

      <div className="flex flex-wrap items-end gap-3">
        <div className="min-w-56 flex-1">
          <Input
            placeholder={t('enrollments.filters.searchPlaceholder')}
            value={searchInput}
            onChange={(e) => setSearchInput(e.target.value)}
          />
        </div>
        <Select
          value={courseId}
          onValueChange={(v) => updateParam('courseId', v === 'any' ? '' : v)}
        >
          <SelectTrigger className="w-48">
            <SelectValue placeholder={t('enrollments.filters.course')} />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="any">{t('enrollments.filters.any')}</SelectItem>
            {courses?.map((c) => (
              <SelectItem key={c.id} value={c.id}>
                {c.name}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        <Select value={status} onValueChange={(v) => updateParam('status', v === 'any' ? '' : v)}>
          <SelectTrigger className="w-40">
            <SelectValue placeholder={t('enrollments.columns.status')} />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="any">{t('enrollments.filters.any')}</SelectItem>
            {STATUSES.map((s) => (
              <SelectItem key={s} value={s}>
                {enumLabel(t, 'enrollmentStatus', s)}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        {(status || courseId || searchInput) && (
          <Button
            variant="ghost"
            onClick={() => {
              setSearchInput('')
              const next = new URLSearchParams(searchParams)
              next.delete('status')
              next.delete('courseId')
              next.delete('page')
              setSearchParams(next, { replace: true })
            }}
          >
            {t('enrollments.filters.clear')}
          </Button>
        )}
      </div>

      <div className={isPlaceholderData ? 'opacity-60 transition-opacity' : undefined}>
        <DataTable
          columns={columns}
          rows={rows}
          rowKey={(row) => row.id}
          isLoading={isPending}
          onRowClick={(row) => navigate(`/enrollments/${row.id}`)}
        />
      </div>

      {data && (
        <Pagination
          page={toNum(data.page)}
          totalPages={toNum(data.totalPages)}
          totalCount={toNum(data.totalCount)}
          onPageChange={goToPage}
        />
      )}

      <CreateEnrollmentDialog
        open={createOpen}
        onOpenChange={setCreateOpen}
        onCreated={() => queryClient.invalidateQueries({ queryKey: ['enrollments'] })}
      />
    </div>
  )
}
