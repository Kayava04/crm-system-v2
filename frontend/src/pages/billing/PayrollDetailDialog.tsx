import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useQuery } from '@tanstack/react-query'
import { toast } from 'sonner'
import { getPayrollById, markPayrollPaid } from '@/features/billing/api'
import type { ResolvedPayrollRow } from '@/features/billing/useResolvedPayrolls'
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

function statusVariant(status: string): 'success' | 'warning' {
  return status === 'Paid' ? 'success' : 'warning'
}

interface PayrollDetailDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  resolved: ResolvedPayrollRow | null
  onChanged: () => void
}

export function PayrollDetailDialog({
  open,
  onOpenChange,
  resolved,
  onChanged,
}: PayrollDetailDialogProps) {
  const { t, i18n } = useTranslation()
  const lang = i18n.language === 'en' ? 'en' : 'uk'
  const canManage = useCan('CanManagePayments')
  const [markPaidOpen, setMarkPaidOpen] = useState(false)

  const { data: detail } = useQuery({
    queryKey: ['payrolls', resolved?.row.id, 'detail'],
    queryFn: () => getPayrollById(resolved!.row.id),
    enabled: open && !!resolved,
  })

  if (!resolved) return null
  const { row } = resolved
  const status = detail?.status ?? row.status

  async function handleMarkPaid() {
    await markPayrollPaid(row.id)
    toast.success(t('billing.payroll.detail.markPaidSuccess'))
    onChanged()
    onOpenChange(false)
  }

  return (
    <>
      <Dialog open={open} onOpenChange={onOpenChange}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              {t('billing.payroll.detail.title')}
              <Badge variant={statusVariant(status)}>{enumLabel(t, 'payrollStatus', status)}</Badge>
            </DialogTitle>
          </DialogHeader>
          <div className="flex flex-col gap-3 text-sm">
            <div className="flex justify-between">
              <span className="text-muted-foreground">{t('billing.payroll.detail.teacher')}</span>
              <span>{resolved.employeeName ?? '—'}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-muted-foreground">{t('billing.payroll.detail.period')}</span>
              <span>{row.period}</span>
            </div>
            {/* A staff payroll (row.teacherId is null) is just the flat Salary for the period -
             * base salary / lessons rate / lessons count are a teacher-only breakdown. */}
            {detail && row.teacherId && (
              <>
                <div className="flex justify-between">
                  <span className="text-muted-foreground">
                    {t('billing.payroll.detail.baseSalary')}
                  </span>
                  <span>{formatCurrency(detail.baseSalary, lang)}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-muted-foreground">
                    {t('billing.payroll.detail.lessonsRate')}
                  </span>
                  <span>{formatCurrency(detail.lessonsRate, lang)}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-muted-foreground">
                    {t('billing.payroll.detail.lessonsCount')}
                  </span>
                  <span>{Number(row.completedLessonsCount)}</span>
                </div>
              </>
            )}
            <div className="flex justify-between font-medium">
              <span className="text-muted-foreground">
                {t('billing.payroll.detail.totalAmount')}
              </span>
              <span>{formatCurrency(row.totalAmount, lang)}</span>
            </div>
            {detail?.paidAt && (
              <div className="flex justify-between">
                <span className="text-muted-foreground">{t('billing.payroll.detail.paidAt')}</span>
                <span>{formatDate(detail.paidAt, lang)}</span>
              </div>
            )}
          </div>

          {canManage && status !== 'Paid' && (
            <DialogFooter>
              <Button onClick={() => setMarkPaidOpen(true)}>
                {t('billing.payroll.detail.markPaid')}
              </Button>
            </DialogFooter>
          )}
        </DialogContent>
      </Dialog>

      <ConfirmDialog
        open={markPaidOpen}
        onOpenChange={setMarkPaidOpen}
        title={t('billing.payroll.detail.confirmMarkPaidTitle')}
        description={t('billing.payroll.detail.confirmMarkPaidDesc')}
        onConfirm={handleMarkPaid}
      />
    </>
  )
}
