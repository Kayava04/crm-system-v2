import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useQuery } from '@tanstack/react-query'
import { getMyInvoices } from '@/features/billing/api'
import { getMyEnrollments } from '@/features/enrollments/api'
import type { InvoiceListItem } from '@/features/billing/api'
import { enumLabel } from '@/lib/enumLabels'
import { formatCurrency, formatDate } from '@/lib/utils'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'

function statusVariant(status: string): 'success' | 'warning' | 'destructive' {
  if (status === 'Paid') return 'success'
  if (status === 'Overdue') return 'destructive'
  return 'warning'
}

export function MyInvoicesPage() {
  const { t, i18n } = useTranslation()
  const lang = i18n.language === 'en' ? 'en' : 'uk'
  const [selected, setSelected] = useState<InvoiceListItem | null>(null)

  const { data: invoices, isLoading } = useQuery({
    queryKey: ['invoices', 'my'],
    queryFn: () => getMyInvoices(),
  })

  // GET /api/billing/invoices/{id} requires CanViewPayments, which students
  // don't hold — only GET /api/billing/invoices/my (role-gated) is reachable
  // for them. That list doesn't carry a course name, but GET
  // /api/enrollments/my (also role-gated, already safe for a student) does,
  // so the course is resolved client-side via the invoice's enrollmentId
  // instead of a second, forbidden request.
  const { data: enrollments } = useQuery({
    queryKey: ['enrollments', 'my'],
    queryFn: getMyEnrollments,
  })
  const courseNameByEnrollmentId = new Map(enrollments?.map((e) => [e.id, e.courseName]) ?? [])

  return (
    <div className="flex max-w-2xl flex-col gap-4">
      <h1 className="text-2xl font-semibold tracking-tight">{t('nav.myInvoices')}</h1>

      {isLoading && (
        <div className="flex flex-col gap-3">
          <Skeleton className="h-24 w-full" />
          <Skeleton className="h-24 w-full" />
        </div>
      )}

      {invoices && invoices.items.length === 0 && (
        <p className="text-sm text-muted-foreground">{t('billing.invoices.empty')}</p>
      )}

      <div className="flex flex-col gap-3">
        {invoices?.items.map((inv) => (
          <Card
            key={inv.id}
            className="cursor-pointer transition-colors hover:bg-muted/40"
            onClick={() => setSelected(inv)}
          >
            <CardHeader className="flex flex-row items-center justify-between">
              <CardTitle className="text-base font-medium">{inv.period}</CardTitle>
              <Badge variant={statusVariant(inv.status)}>
                {enumLabel(t, 'invoiceStatus', inv.status)}
              </Badge>
            </CardHeader>
            <CardContent className="flex flex-wrap gap-x-6 gap-y-1 text-sm text-muted-foreground">
              <span>{formatCurrency(inv.amount, lang)}</span>
              <span>
                {t('billing.invoices.columns.dueDate')}: {formatDate(inv.dueDate, lang)}
              </span>
            </CardContent>
          </Card>
        ))}
      </div>

      <MyInvoiceDetailDialog
        open={!!selected}
        onOpenChange={(open) => !open && setSelected(null)}
        invoice={selected}
        courseName={selected ? courseNameByEnrollmentId.get(selected.enrollmentId) : undefined}
        lang={lang}
      />
    </div>
  )
}

function MyInvoiceDetailDialog({
  open,
  onOpenChange,
  invoice,
  courseName,
  lang,
}: {
  open: boolean
  onOpenChange: (open: boolean) => void
  invoice: InvoiceListItem | null
  courseName: string | undefined
  lang: string
}) {
  const { t } = useTranslation()
  if (!invoice) return null

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            {t('billing.invoices.detail.title')}
            <Badge variant={statusVariant(invoice.status)}>
              {enumLabel(t, 'invoiceStatus', invoice.status)}
            </Badge>
          </DialogTitle>
        </DialogHeader>
        <div className="flex flex-col gap-3 text-sm">
          <div className="flex justify-between">
            <span className="text-muted-foreground">{t('billing.invoices.detail.course')}</span>
            <span>{courseName ?? '—'}</span>
          </div>
          <div className="flex justify-between">
            <span className="text-muted-foreground">{t('billing.invoices.detail.period')}</span>
            <span>{invoice.period}</span>
          </div>
          <div className="flex justify-between">
            <span className="text-muted-foreground">{t('billing.invoices.detail.amount')}</span>
            <span>{formatCurrency(invoice.amount, lang)}</span>
          </div>
          <div className="flex justify-between">
            <span className="text-muted-foreground">{t('billing.invoices.detail.dueDate')}</span>
            <span>{formatDate(invoice.dueDate, lang)}</span>
          </div>
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            {t('common.close')}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
