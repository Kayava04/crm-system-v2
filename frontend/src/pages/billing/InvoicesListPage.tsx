import { useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useQuery, useQueryClient, keepPreviousData } from '@tanstack/react-query'
import { toast } from 'sonner'
import { Plus, AlertTriangle } from 'lucide-react'
import { getInvoices, markInvoicesOverdue } from '@/features/billing/api'
import type { InvoiceStatus } from '@/features/billing/api'
import {
  useResolvedInvoices,
  type ResolvedInvoiceRow,
} from '@/features/billing/useResolvedInvoices'
import { useCan } from '@/features/auth/useCan'
import { enumLabel } from '@/lib/enumLabels'
import { toNum, formatCurrency, formatDate } from '@/lib/utils'
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
import { CreateInvoiceDialog } from './CreateInvoiceDialog'
import { InvoiceDetailDialog } from './InvoiceDetailDialog'

const STATUSES: InvoiceStatus[] = ['Pending', 'Paid', 'Overdue']

function statusVariant(status: string): 'success' | 'warning' | 'destructive' {
  if (status === 'Paid') return 'success'
  if (status === 'Overdue') return 'destructive'
  return 'warning'
}

export function InvoicesListPage() {
  const { t, i18n } = useTranslation()
  const lang = i18n.language === 'en' ? 'en' : 'uk'
  const queryClient = useQueryClient()
  const canManage = useCan('CanManagePayments')

  const [page, setPage] = useState(1)
  const [status, setStatus] = useState('')
  const [createOpen, setCreateOpen] = useState(false)
  const [selected, setSelected] = useState<ResolvedInvoiceRow | null>(null)

  const filters = useMemo(
    () => ({ status: (status || undefined) as InvoiceStatus | undefined, page, pageSize: 20 }),
    [status, page],
  )

  const { data, isPending, isPlaceholderData } = useQuery({
    queryKey: ['invoices', filters],
    queryFn: () => getInvoices(filters),
    placeholderData: keepPreviousData,
  })

  const resolved = useResolvedInvoices(data?.items ?? [])

  function invalidate() {
    queryClient.invalidateQueries({ queryKey: ['invoices'] })
  }

  async function handleMarkOverdue() {
    const result = await markInvoicesOverdue()
    toast.success(t('billing.invoices.markOverdueResult', { count: Number(result.updatedCount) }))
    invalidate()
  }

  const columns: DataTableColumn<ResolvedInvoiceRow>[] = [
    {
      key: 'student',
      header: t('billing.invoices.columns.student'),
      cell: (row) => <span className="font-medium">{row.studentName ?? '—'}</span>,
    },
    {
      key: 'course',
      header: t('billing.invoices.columns.course'),
      cell: (row) => row.courseName ?? '—',
    },
    {
      key: 'period',
      header: t('billing.invoices.columns.period'),
      cell: (row) => row.row.period,
    },
    {
      key: 'amount',
      header: t('billing.invoices.columns.amount'),
      cell: (row) => formatCurrency(row.row.amount, lang),
    },
    {
      key: 'dueDate',
      header: t('billing.invoices.columns.dueDate'),
      cell: (row) => formatDate(row.row.dueDate, lang),
    },
    {
      key: 'status',
      header: t('billing.invoices.columns.status'),
      cell: (row) => (
        <Badge variant={statusVariant(row.row.status)}>
          {enumLabel(t, 'invoiceStatus', row.row.status)}
        </Badge>
      ),
    },
  ]

  return (
    <div className="flex flex-col gap-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-2xl font-semibold tracking-tight">{t('billing.invoices.title')}</h1>
        {canManage && (
          <div className="flex flex-wrap gap-2">
            <Button onClick={() => setCreateOpen(true)}>
              <Plus />
              {t('billing.invoices.add')}
            </Button>
            <Button variant="outline" onClick={handleMarkOverdue}>
              <AlertTriangle />
              {t('billing.invoices.markOverdue')}
            </Button>
          </div>
        )}
      </div>

      <div className="flex flex-wrap items-end gap-3">
        <Select
          value={status}
          onValueChange={(v) => {
            setStatus(v === 'any' ? '' : v)
            setPage(1)
          }}
        >
          <SelectTrigger className="w-40">
            <SelectValue placeholder={t('billing.invoices.filters.status')} />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="any">{t('billing.invoices.filters.any')}</SelectItem>
            {STATUSES.map((s) => (
              <SelectItem key={s} value={s}>
                {enumLabel(t, 'invoiceStatus', s)}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        {status && (
          <Button
            variant="ghost"
            onClick={() => {
              setStatus('')
              setPage(1)
            }}
          >
            {t('billing.invoices.filters.clear')}
          </Button>
        )}
      </div>

      <div className={isPlaceholderData ? 'opacity-60 transition-opacity' : undefined}>
        <DataTable
          columns={columns}
          rows={resolved}
          rowKey={(row) => row.row.id}
          isLoading={isPending}
          emptyMessage={t('billing.invoices.empty')}
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

      <CreateInvoiceDialog open={createOpen} onOpenChange={setCreateOpen} onCreated={invalidate} />
      <InvoiceDetailDialog
        open={!!selected}
        onOpenChange={(open) => !open && setSelected(null)}
        resolved={selected}
        onChanged={invalidate}
      />
    </div>
  )
}
