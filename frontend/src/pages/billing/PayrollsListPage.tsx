import { useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useQuery, useQueryClient, keepPreviousData } from '@tanstack/react-query'
import { Plus } from 'lucide-react'
import { getPayrolls } from '@/features/billing/api'
import type { PayrollStatus } from '@/features/billing/api'
import {
  useResolvedPayrolls,
  type ResolvedPayrollRow,
} from '@/features/billing/useResolvedPayrolls'
import { useCan } from '@/features/auth/useCan'
import { enumLabel } from '@/lib/enumLabels'
import { toNum, formatCurrency } from '@/lib/utils'
import { Button } from '@/components/ui/button'
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
import { EmployeeSearchInput, type EmployeePick } from '@/components/shared/EmployeeSearchInput'
import { CreatePayrollDialog } from './CreatePayrollDialog'
import { PayrollDetailDialog } from './PayrollDetailDialog'

const STATUSES: PayrollStatus[] = ['Pending', 'Paid']

function statusVariant(status: string): 'success' | 'warning' {
  return status === 'Paid' ? 'success' : 'warning'
}

export function PayrollsListPage() {
  const { t, i18n } = useTranslation()
  const lang = i18n.language === 'en' ? 'en' : 'uk'
  const queryClient = useQueryClient()
  const canManage = useCan('CanManagePayments')

  const [page, setPage] = useState(1)
  const [status, setStatus] = useState('')
  const [employee, setEmployee] = useState<EmployeePick | null>(null)
  const [createOpen, setCreateOpen] = useState(false)
  const [selected, setSelected] = useState<ResolvedPayrollRow | null>(null)

  const filters = useMemo(
    () => ({
      status: (status || undefined) as PayrollStatus | undefined,
      teacherId: employee?.kind === 'teacher' ? employee.id : undefined,
      userId: employee?.kind === 'staff' ? employee.id : undefined,
      page,
      pageSize: 20,
    }),
    [status, employee, page],
  )

  const { data, isPending, isPlaceholderData } = useQuery({
    queryKey: ['payrolls', filters],
    queryFn: () => getPayrolls(filters),
    placeholderData: keepPreviousData,
  })

  const resolved = useResolvedPayrolls(data?.items ?? [])

  function invalidate() {
    queryClient.invalidateQueries({ queryKey: ['payrolls'] })
  }

  const hasFilters = !!(status || employee)

  const columns: DataTableColumn<ResolvedPayrollRow>[] = [
    {
      key: 'teacher',
      header: t('billing.payroll.columns.teacher'),
      cell: (row) => <span className="font-medium">{row.employeeName ?? '—'}</span>,
    },
    {
      key: 'period',
      header: t('billing.payroll.columns.period'),
      cell: (row) => row.row.period,
    },
    {
      key: 'lessonsCount',
      header: t('billing.payroll.columns.lessonsCount'),
      cell: (row) => Number(row.row.completedLessonsCount),
    },
    {
      key: 'amount',
      header: t('billing.payroll.columns.amount'),
      cell: (row) => formatCurrency(row.row.totalAmount, lang),
    },
    {
      key: 'status',
      header: t('billing.payroll.columns.status'),
      cell: (row) => (
        <Badge variant={statusVariant(row.row.status)}>
          {enumLabel(t, 'payrollStatus', row.row.status)}
        </Badge>
      ),
    },
  ]

  return (
    <div className="flex flex-col gap-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-2xl font-semibold tracking-tight">{t('billing.payroll.title')}</h1>
        {canManage && (
          <Button onClick={() => setCreateOpen(true)}>
            <Plus />
            {t('billing.payroll.add')}
          </Button>
        )}
      </div>

      <div className="flex flex-wrap items-end gap-3">
        <div className="min-w-56 flex-1">
          <EmployeeSearchInput
            value={employee}
            onChange={setEmployee}
            placeholder={t('billing.payroll.filters.teacher')}
          />
        </div>
        <Select
          value={status}
          onValueChange={(v) => {
            setStatus(v === 'any' ? '' : v)
            setPage(1)
          }}
        >
          <SelectTrigger className="w-40">
            <SelectValue placeholder={t('billing.payroll.filters.status')} />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="any">{t('billing.payroll.filters.any')}</SelectItem>
            {STATUSES.map((s) => (
              <SelectItem key={s} value={s}>
                {enumLabel(t, 'payrollStatus', s)}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        {hasFilters && (
          <Button
            variant="ghost"
            onClick={() => {
              setStatus('')
              setEmployee(null)
              setPage(1)
            }}
          >
            {t('billing.payroll.filters.clear')}
          </Button>
        )}
      </div>

      <div className={isPlaceholderData ? 'opacity-60 transition-opacity' : undefined}>
        <DataTable
          columns={columns}
          rows={resolved}
          rowKey={(row) => row.row.id}
          isLoading={isPending}
          emptyMessage={t('billing.payroll.empty')}
          onRowClick={(row) => setSelected(row)}
        />
      </div>

      {data && (
        <Pagination
          page={toNum(data.page)}
          totalPages={toNum(data.totalPages)}
          totalCount={toNum(data.totalCount)}
          onPageChange={setPage}
        />
      )}

      <CreatePayrollDialog open={createOpen} onOpenChange={setCreateOpen} onCreated={invalidate} />
      <PayrollDetailDialog
        open={!!selected}
        onOpenChange={(open) => !open && setSelected(null)}
        resolved={selected}
        onChanged={invalidate}
      />
    </div>
  )
}
