import { useTranslation } from 'react-i18next'
import { useQuery } from '@tanstack/react-query'
import { getMyPayrolls } from '@/features/billing/api'
import { enumLabel } from '@/lib/enumLabels'
import { formatCurrency } from '@/lib/utils'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'

function statusVariant(status: string): 'success' | 'warning' {
  return status === 'Paid' ? 'success' : 'warning'
}

export function MyPayrollsPage() {
  const { t, i18n } = useTranslation()
  const lang = i18n.language === 'en' ? 'en' : 'uk'

  const { data: payrolls, isLoading } = useQuery({
    queryKey: ['payrolls', 'my'],
    queryFn: () => getMyPayrolls(),
  })

  return (
    <div className="flex max-w-2xl flex-col gap-4">
      <h1 className="text-2xl font-semibold tracking-tight">{t('nav.myPayroll')}</h1>

      {isLoading && (
        <div className="flex flex-col gap-3">
          <Skeleton className="h-24 w-full" />
          <Skeleton className="h-24 w-full" />
        </div>
      )}

      {payrolls && payrolls.items.length === 0 && (
        <p className="text-sm text-muted-foreground">{t('billing.payroll.empty')}</p>
      )}

      <div className="flex flex-col gap-3">
        {payrolls?.items.map((p) => (
          <Card key={p.id}>
            <CardHeader className="flex flex-row items-center justify-between">
              <CardTitle className="text-base font-medium">{p.period}</CardTitle>
              <Badge variant={statusVariant(p.status)}>
                {enumLabel(t, 'payrollStatus', p.status)}
              </Badge>
            </CardHeader>
            <CardContent className="flex flex-wrap gap-x-6 gap-y-1 text-sm text-muted-foreground">
              <span className="font-medium text-foreground">
                {formatCurrency(p.totalAmount, lang)}
              </span>
              <span>
                {t('billing.payroll.columns.lessonsCount')}: {Number(p.completedLessonsCount)}
              </span>
            </CardContent>
          </Card>
        ))}
      </div>
    </div>
  )
}
