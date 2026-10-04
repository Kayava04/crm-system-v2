import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useQuery } from '@tanstack/react-query'
import { Wallet, TrendingDown, Scale, FileClock, AlertTriangle, Landmark } from 'lucide-react'
import {
  getBillingSummary,
  getEnrollmentsSummary,
  getStudentsSummary,
  getTeachersSummary,
} from '@/features/reports/api'
import { toNum, formatDate } from '@/lib/utils'
import { Button } from '@/components/ui/button'
import { DateRangePicker } from '@/components/ui/date-range-picker'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import { Field } from '@/components/shared/Field'
import { StatTile, MoneyStatTile } from './charts/StatTile'
import { StatusDonut } from './charts/StatusDonut'
import { RankingBars } from './charts/RankingBars'
import { BalanceRings } from './charts/BalanceRings'

// Every status a category can have, in a FIXED order — a status keeps the
// same color across renders and filters ("color follows the entity, never
// its rank"), instead of being colored by its position among only the
// statuses that happen to have a nonzero count this time.
const STUDENT_STATUS_ORDER = ['Active', 'Suspended', 'Graduated', 'Withdrawn'] as const
const TEACHER_STATUS_ORDER = ['Probation', 'Employed', 'OnLeave', 'Resigned', 'Dismissed'] as const
const ENROLLMENT_STATUS_ORDER = ['Draft', 'Active', 'Suspended', 'Completed', 'Terminated'] as const

export function DashboardPage() {
  const { t, i18n } = useTranslation()
  const lang = i18n.language === 'en' ? 'en' : 'uk'

  const [dateFrom, setDateFrom] = useState('')
  const [dateTo, setDateTo] = useState('')
  const [appliedRange, setAppliedRange] = useState<{ from?: string; to?: string }>({})

  const { data: billing, isLoading: billingLoading } = useQuery({
    queryKey: ['reports', 'billing', appliedRange],
    queryFn: () => getBillingSummary(appliedRange.from, appliedRange.to),
  })
  const { data: students, isLoading: studentsLoading } = useQuery({
    queryKey: ['reports', 'students'],
    queryFn: getStudentsSummary,
  })
  const { data: teachers, isLoading: teachersLoading } = useQuery({
    queryKey: ['reports', 'teachers'],
    queryFn: getTeachersSummary,
  })
  const { data: enrollments, isLoading: enrollmentsLoading } = useQuery({
    queryKey: ['reports', 'enrollments'],
    queryFn: getEnrollmentsSummary,
  })

  const byCourseData = (enrollments?.byCourse ?? [])
    .map((c) => ({ name: c.courseName, value: toNum(c.count) }))
    .sort((a, b) => b.value - a.value)
    .slice(0, 10)

  const netPositive = billing ? toNum(billing.netResult) >= 0 : true

  return (
    <div className="flex flex-col gap-6">
      <h1>{t('reports.title')}</h1>

      <Card>
        <CardHeader>
          <CardTitle>{t('reports.billing.title')}</CardTitle>
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          <div className="flex flex-col items-stretch gap-3 sm:flex-row sm:flex-wrap sm:items-end">
            <div className="w-full sm:w-72">
              <Field label={t('reports.period')}>
                <DateRangePicker
                  value={{ from: dateFrom, to: dateTo }}
                  onChange={(range) => {
                    setDateFrom(range.from)
                    setDateTo(range.to)
                    setAppliedRange({ from: range.from || undefined, to: range.to || undefined })
                  }}
                />
              </Field>
            </div>
            <div className="flex gap-3">
              {(dateFrom || dateTo) && (
                <Button
                  size="sm"
                  variant="ghost"
                  onClick={() => {
                    setDateFrom('')
                    setDateTo('')
                    setAppliedRange({})
                  }}
                >
                  {t('reports.resetToCurrentMonth')}
                </Button>
              )}
            </div>
          </div>

          {billingLoading && <Skeleton className="h-24 w-full" />}
          {billing && (
            <>
              <p className="w-fit rounded-full bg-muted px-2.5 py-1 text-xs text-muted-foreground tabular-nums">
                {formatDate(billing.dateFrom, lang)} – {formatDate(billing.dateTo, lang)}
              </p>

              <div className="grid grid-cols-1 gap-4 lg:grid-cols-[1fr_auto]">
                <div className="grid grid-cols-2 gap-3 sm:grid-cols-3">
                  <MoneyStatTile
                    label={t('reports.billing.income')}
                    amount={billing.income}
                    lang={lang}
                    icon={Wallet}
                    tone="success"
                  />
                  <MoneyStatTile
                    label={t('reports.billing.expenses')}
                    amount={billing.expenses}
                    lang={lang}
                    icon={TrendingDown}
                    tone="destructive"
                  />
                  <MoneyStatTile
                    label={t('reports.billing.netResult')}
                    amount={billing.netResult}
                    lang={lang}
                    icon={Scale}
                    tone={netPositive ? 'success' : 'destructive'}
                  />
                  <MoneyStatTile
                    label={t('reports.billing.pendingInvoices')}
                    amount={billing.pendingInvoicesAmount}
                    lang={lang}
                    icon={FileClock}
                    tone="warning"
                  />
                  <MoneyStatTile
                    label={t('reports.billing.overdueInvoices')}
                    amount={billing.overdueInvoicesAmount}
                    lang={lang}
                    icon={AlertTriangle}
                    tone="destructive"
                  />
                  <MoneyStatTile
                    label={t('reports.billing.pendingPayroll')}
                    amount={billing.pendingPayrollAmount}
                    lang={lang}
                    icon={Landmark}
                    tone="warning"
                  />
                </div>

                <div className="flex w-full animate-rise-in flex-col gap-4 rounded-xl bg-muted/60 p-5 lg:w-72">
                  <p className="text-sm font-medium">{t('reports.billing.incomeVsExpenses')}</p>
                  <BalanceRings
                    income={billing.income}
                    expenses={billing.expenses}
                    net={billing.netResult}
                    lang={lang}
                  />
                </div>
              </div>
            </>
          )}
        </CardContent>
      </Card>

      <div className="grid grid-cols-1 items-start gap-6 lg:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>{t('reports.students.title')}</CardTitle>
          </CardHeader>
          <CardContent className="flex flex-col gap-4">
            {studentsLoading && <Skeleton className="h-32 w-full" />}
            {students && (
              <>
                <div>
                  <p className="mb-3 text-sm font-medium text-muted-foreground">
                    {t('reports.students.byStatus')}
                  </p>
                  <StatusDonut
                    data={students.byStatus}
                    category="studentStatus"
                    order={STUDENT_STATUS_ORDER}
                  />
                </div>
              </>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>{t('reports.teachers.title')}</CardTitle>
          </CardHeader>
          <CardContent className="flex flex-col gap-4">
            {teachersLoading && <Skeleton className="h-32 w-full" />}
            {teachers && (
              <>
                <div>
                  <p className="mb-3 text-sm font-medium text-muted-foreground">
                    {t('reports.teachers.byStatus')}
                  </p>
                  <StatusDonut
                    data={teachers.byStatus}
                    category="teacherStatus"
                    order={TEACHER_STATUS_ORDER}
                  />
                </div>
                <div className="flex flex-col gap-2">
                  <p className="text-sm text-muted-foreground">
                    {t('reports.teachers.salaryOverview.title')}
                  </p>
                  <div className="grid grid-cols-2 gap-3">
                    <StatTile
                      label={t('reports.teachers.salaryOverview.teachersWithRate')}
                      value={toNum(teachers.salaryOverview.teachersWithRate)}
                    />
                    <MoneyStatTile
                      label={t('reports.teachers.salaryOverview.totalBaseSalary')}
                      amount={teachers.salaryOverview.totalBaseSalary}
                      lang={lang}
                    />
                    <MoneyStatTile
                      label={t('reports.teachers.salaryOverview.averageBaseSalary')}
                      amount={teachers.salaryOverview.averageBaseSalary}
                      lang={lang}
                    />
                    <MoneyStatTile
                      label={t('reports.teachers.salaryOverview.averageLessonsRate')}
                      amount={teachers.salaryOverview.averageLessonsRate}
                      lang={lang}
                    />
                  </div>
                </div>
              </>
            )}
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>{t('reports.enrollments.title')}</CardTitle>
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          {enrollmentsLoading && <Skeleton className="h-32 w-full" />}
          {enrollments && (
            <>
              <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
                <div>
                  <p className="mb-3 text-sm font-medium text-muted-foreground">
                    {t('reports.enrollments.byStatus')}
                  </p>
                  <StatusDonut
                    data={enrollments.byStatus}
                    category="enrollmentStatus"
                    order={ENROLLMENT_STATUS_ORDER}
                  />
                </div>
                <div>
                  <p className="mb-3 text-sm font-medium text-muted-foreground">
                    {t('reports.enrollments.byCourse')}
                  </p>
                  <RankingBars data={byCourseData} />
                </div>
              </div>
            </>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
