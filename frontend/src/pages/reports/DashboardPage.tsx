import { useState, type ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { useQuery } from '@tanstack/react-query'
import { Wallet, TrendingDown, Scale, FileClock, AlertTriangle, Landmark } from 'lucide-react'
import {
  getBillingSummary,
  getEnrollmentsSummary,
  getStudentsSummary,
  getTeachersSummary,
} from '@/features/reports/api'
import { useCan } from '@/features/auth/useCan'
import { toNum, formatDate, cn } from '@/lib/utils'
import { Button } from '@/components/ui/button'
import { DateRangePicker } from '@/components/ui/date-range-picker'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import { Field } from '@/components/shared/Field'
import { StatTile, MoneyStatTile } from './charts/StatTile'
import { StatusDonut } from './charts/StatusDonut'
import { RankingBars } from './charts/RankingBars'
import { BalanceRings } from './charts/BalanceRings'
import { FinanceTrend } from './charts/FinanceTrend'
import { LessonsByWeekday } from './charts/LessonsByWeekday'

// Every status a category can have, in a FIXED order — a status keeps the
// same color across renders and filters ("color follows the entity, never
// its rank"), instead of being colored by its position among only the
// statuses that happen to have a nonzero count this time.
const STUDENT_STATUS_ORDER = ['Active', 'Suspended', 'Graduated', 'Withdrawn'] as const
const TEACHER_STATUS_ORDER = ['Probation', 'Employed', 'OnLeave', 'Resigned', 'Dismissed'] as const
const ENROLLMENT_STATUS_ORDER = ['Draft', 'Active', 'Suspended', 'Completed', 'Terminated'] as const

function DashCard({
  title,
  description,
  className,
  children,
}: {
  title: string
  description?: string
  className?: string
  children: ReactNode
}) {
  return (
    <Card className={cn('flex flex-col', className)}>
      <CardHeader>
        <CardTitle>{title}</CardTitle>
        {description && <CardDescription>{description}</CardDescription>}
      </CardHeader>
      <CardContent className="flex flex-1 flex-col gap-4">{children}</CardContent>
    </Card>
  )
}

export function DashboardPage() {
  const { t, i18n } = useTranslation()
  const lang = i18n.language === 'en' ? 'en' : 'uk'
  const canViewSchedule = useCan('CanViewSchedule')

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

      <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
        <DashCard
          title={t('reports.trend.title')}
          description={t('reports.trend.subtitle')}
          className={canViewSchedule ? 'lg:col-span-2' : 'lg:col-span-3'}
        >
          <FinanceTrend lang={lang} />
        </DashCard>
        {canViewSchedule && (
          <DashCard title={t('reports.lessons.title')} description={t('reports.lessons.subtitle')}>
            <LessonsByWeekday lang={lang} />
          </DashCard>
        )}
      </div>

      <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
        <DashCard title={t('reports.students.title')} description={t('reports.students.byStatus')}>
          {studentsLoading && <Skeleton className="h-44 w-full" />}
          {students && (
            <StatusDonut
              data={students.byStatus}
              category="studentStatus"
              order={STUDENT_STATUS_ORDER}
            />
          )}
        </DashCard>
        <DashCard title={t('reports.teachers.title')} description={t('reports.teachers.byStatus')}>
          {teachersLoading && <Skeleton className="h-44 w-full" />}
          {teachers && (
            <StatusDonut
              data={teachers.byStatus}
              category="teacherStatus"
              order={TEACHER_STATUS_ORDER}
            />
          )}
        </DashCard>
      </div>

      <DashCard
        title={t('reports.enrollments.title')}
        description={
          enrollments ? t('reports.enrollments.total') + ': ' + toNum(enrollments.total) : undefined
        }
      >
        {enrollmentsLoading && <Skeleton className="h-44 w-full" />}
        {enrollments && (
          <div className="grid grid-cols-1 gap-8 lg:grid-cols-2 lg:gap-0">
            <section className="flex flex-col gap-4 lg:pr-8">
              <h4 className="text-sm font-medium text-muted-foreground">
                {t('reports.enrollments.byStatus')}
              </h4>
              <StatusDonut
                data={enrollments.byStatus}
                category="enrollmentStatus"
                order={ENROLLMENT_STATUS_ORDER}
              />
            </section>
            <section className="flex flex-col gap-4 border-t border-foreground/8 pt-8 lg:border-t-0 lg:border-l lg:pt-0 lg:pl-8">
              <h4 className="text-sm font-medium text-muted-foreground">
                {t('reports.enrollments.byCourse')}
              </h4>
              <RankingBars data={byCourseData} />
            </section>
          </div>
        )}
      </DashCard>

      <DashCard title={t('reports.teachers.salaryOverview.title')}>
        {teachersLoading && <Skeleton className="h-24 w-full" />}
        {teachers && (
          <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
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
        )}
      </DashCard>
    </div>
  )
}
