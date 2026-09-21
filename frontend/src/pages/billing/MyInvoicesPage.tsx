import { useTranslation } from 'react-i18next'
import { useQuery } from '@tanstack/react-query'
import { getMyInvoices } from '@/features/billing/api'
import { enumLabel } from '@/lib/enumLabels'
import { formatCurrency, formatDate } from '@/lib/utils'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'

function statusVariant(status: string): 'success' | 'warning' | 'destructive' {
  if (status === 'Paid') return 'success'
  if (status === 'Overdue') return 'destructive'
  return 'warning'
}

export function MyInvoicesPage() {
  const { t, i18n } = useTranslation()
  const lang = i18n.language === 'en' ? 'en' : 'uk'

  const { data: invoices, isLoading } = useQuery({
    queryKey: ['invoices', 'my'],
    queryFn: () => getMyInvoices(),
  })

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
          <Card key={inv.id}>
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
    </div>
  )
}
