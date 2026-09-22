import { useTranslation } from 'react-i18next'
import { useQuery } from '@tanstack/react-query'
import { useAuth } from '@/features/auth/useAuth'
import { getMyPayrolls } from '@/features/billing/api'
import { enumLabel } from '@/lib/enumLabels'
import { formatCurrency } from '@/lib/utils'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'

function statusVariant(status: string): 'success' | 'warning' {
  return status === 'Paid' ? 'success' : 'warning'
}

/** An administrator's or manager's own pay: the current Salary figure (set by another
 * CanManageAdmins holder, see staff management - never here) and the history of payroll
 * accrued for them, the same "Нарахувати зарплату" mechanism a teacher's payroll already uses. */
export function SalaryTab() {
  const { t, i18n } = useTranslation()
  const lang = i18n.language === 'en' ? 'en' : 'uk'
  const { user } = useAuth()

  const { data: payrolls, isLoading } = useQuery({
    queryKey: ['payrolls', 'my'],
    queryFn: () => getMyPayrolls(),
  })

  return (
    <div className="flex flex-col gap-4">
      <Card>
        <CardHeader>
          <CardTitle className="text-base font-medium">{t('staff.detail.salaryTitle')}</CardTitle>
          <CardDescription>{t('profile.salary.description')}</CardDescription>
        </CardHeader>
        <CardContent>
          {user?.contact?.salary != null ? (
            <p className="text-2xl font-semibold">{formatCurrency(user.contact.salary, lang)}</p>
          ) : (
            <p className="text-sm text-muted-foreground">{t('staff.detail.salaryPlaceholder')}</p>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-base font-medium">{t('profile.salary.historyTitle')}</CardTitle>
        </CardHeader>
        <CardContent className="flex flex-col gap-2">
          {isLoading && (
            <div className="flex flex-col gap-2">
              <Skeleton className="h-10 w-full" />
              <Skeleton className="h-10 w-full" />
            </div>
          )}
          {payrolls && payrolls.items.length === 0 && (
            <p className="text-sm text-muted-foreground">{t('billing.payroll.empty')}</p>
          )}
          {payrolls?.items.map((p) => (
            <div
              key={p.id}
              className="flex flex-wrap items-center justify-between gap-2 rounded-md border p-3 text-sm"
            >
              <span className="font-medium">{p.period}</span>
              <span>{formatCurrency(p.totalAmount, lang)}</span>
              <Badge variant={statusVariant(p.status)}>{enumLabel(t, 'payrollStatus', p.status)}</Badge>
            </div>
          ))}
        </CardContent>
      </Card>
    </div>
  )
}
