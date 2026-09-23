import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useQuery } from '@tanstack/react-query'
import { toast } from 'sonner'
import { getInvoiceById, markInvoicePaid } from '@/features/billing/api'
import type { ResolvedInvoiceRow } from '@/features/billing/useResolvedInvoices'
import { enumLabel } from '@/lib/enumLabels'
import { formatCurrency, formatDate } from '@/lib/utils'
import { useCan } from '@/features/auth/useCan'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { ConfirmDialog } from '@/components/shared/ConfirmDialog'

function statusVariant(status: string): 'success' | 'warning' | 'destructive' {
  if (status === 'Paid') return 'success'
  if (status === 'Overdue') return 'destructive'
  return 'warning'
}

interface InvoiceDetailDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  resolved: ResolvedInvoiceRow | null
  onChanged: () => void
}

export function InvoiceDetailDialog({
  open,
  onOpenChange,
  resolved,
  onChanged,
}: InvoiceDetailDialogProps) {
  const { t, i18n } = useTranslation()
  const lang = i18n.language === 'en' ? 'en' : 'uk'
  const canManage = useCan('CanManagePayments')
  const [markPaidOpen, setMarkPaidOpen] = useState(false)

  const { data: detail } = useQuery({
    queryKey: ['invoices', resolved?.row.id, 'detail'],
    queryFn: () => getInvoiceById(resolved!.row.id),
    enabled: open && !!resolved,
  })

  if (!resolved) return null
  const { row } = resolved
  const status = detail?.status ?? row.status

  async function handleMarkPaid() {
    await markInvoicePaid(row.id)
    toast.success(t('billing.invoices.detail.markPaidSuccess'))
    onChanged()
    onOpenChange(false)
  }

  return (
    <>
      <Dialog open={open} onOpenChange={onOpenChange}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              {t('billing.invoices.detail.title')}
              <Badge variant={statusVariant(status)}>{enumLabel(t, 'invoiceStatus', status)}</Badge>
            </DialogTitle>
          </DialogHeader>
          <div className="flex flex-col gap-3 text-sm">
            <div className="flex justify-between">
              <span className="text-muted-foreground">{t('billing.invoices.detail.student')}</span>
              <span>{resolved.studentName ?? '—'}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-muted-foreground">{t('billing.invoices.detail.course')}</span>
              <span>{resolved.courseName ?? '—'}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-muted-foreground">{t('billing.invoices.detail.period')}</span>
              <span>{row.period}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-muted-foreground">{t('billing.invoices.detail.amount')}</span>
              <span>{formatCurrency(row.amount, lang)}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-muted-foreground">{t('billing.invoices.detail.dueDate')}</span>
              <span>{formatDate(row.dueDate, lang)}</span>
            </div>
            {detail?.paidAt && (
              <div className="flex justify-between">
                <span className="text-muted-foreground">{t('billing.invoices.detail.paidAt')}</span>
                <span>{formatDate(detail.paidAt, lang)}</span>
              </div>
            )}
            {detail?.notes && (
              <div className="flex flex-col gap-1">
                <span className="text-muted-foreground">{t('billing.invoices.detail.notes')}</span>
                <span>{detail.notes}</span>
              </div>
            )}
          </div>

          {canManage && status !== 'Paid' && (
            <DialogFooter>
              <Button onClick={() => setMarkPaidOpen(true)}>
                {t('billing.invoices.detail.markPaid')}
              </Button>
            </DialogFooter>
          )}
        </DialogContent>
      </Dialog>

      <ConfirmDialog
        open={markPaidOpen}
        onOpenChange={setMarkPaidOpen}
        title={t('billing.invoices.detail.confirmMarkPaidTitle')}
        description={t('billing.invoices.detail.confirmMarkPaidDesc')}
        onConfirm={handleMarkPaid}
      />
    </>
  )
}
