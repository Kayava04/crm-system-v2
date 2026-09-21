import { useState, type ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { useQuery } from '@tanstack/react-query'
import { Bar, BarChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import {
  getBillingSummary,
  getEnrollmentsSummary,
  getStudentsSummary,
  getTeachersSummary,
} from '@/features/reports/api'
import { enumLabel, type EnumCategory } from '@/lib/enumLabels'
import { toNum, formatCurrencyParts, formatDate } from '@/lib/utils'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import { Field } from '@/components/shared/Field'

const CHART_COLORS = [
  'var(--chart-1)',
  'var(--chart-2)',
  'var(--chart-3)',
  'var(--chart-4)',
  'var(--chart-5)',
]

function StatCard({
  label,
  value,
  unit,
}: {
  label: string
  value: ReactNode
  /** A smaller, muted suffix next to the value — e.g. the currency symbol,
   * kept visually secondary to the number itself. */
  unit?: string
}) {
  return (
    <Card>
      <CardContent className="flex h-full flex-col gap-1 py-4">
        <span className="line-clamp-2 min-h-10 text-sm leading-tight text-muted-foreground">
          {label}
        </span>
        <span className="flex items-baseline gap-1">
          <span className="text-2xl font-semibold tracking-tight">{value}</span>
          {unit && <span className="text-sm font-normal text-muted-foreground">{unit}</span>}
        </span>
      </CardContent>
    </Card>
  )
}

/** Renders a money StatCard with the currency unit visually smaller than the
 * number, instead of one same-size formatted string. */
function MoneyStatCard({
  label,
  amount,
  lang,
}: {
  label: string
  amount: number | string
  lang: string
}) {
  const { value, unit } = formatCurrencyParts(amount, lang)
  return <StatCard label={label} value={value} unit={unit} />
}

function StatusChart({
  data,
  category,
}: {
  data: Record<string, number | string>
  category: EnumCategory
}) {
  const { t } = useTranslation()
  const chartData = Object.entries(data).map(([status, count], i) => ({
    name: enumLabel(t, category, status),
    value: toNum(count),
    fill: CHART_COLORS[i % CHART_COLORS.length],
  }))

  if (chartData.every((d) => d.value === 0)) {
    return <p className="text-sm text-muted-foreground">—</p>
  }

  return (
    <ResponsiveContainer width="100%" height={Math.max(120, chartData.length * 40)}>
      <BarChart data={chartData} layout="vertical" margin={{ left: 8, right: 16 }}>
        <CartesianGrid horizontal={false} stroke="var(--border)" />
        <XAxis
          type="number"
          allowDecimals={false}
          tick={{ fill: 'var(--muted-foreground)', fontSize: 12 }}
        />
        <YAxis
          type="category"
          dataKey="name"
          width={110}
          tick={{ fill: 'var(--muted-foreground)', fontSize: 12 }}
        />
        <Tooltip
          contentStyle={{
            background: 'var(--popover)',
            border: '1px solid var(--border)',
            borderRadius: 8,
            color: 'var(--popover-foreground)',
            fontSize: 12,
          }}
        />
        <Bar
          dataKey="value"
          radius={[0, 4, 4, 0]}
          animationDuration={500}
          animationEasing="ease-out"
          activeBar={{ fillOpacity: 0.75 }}
        />
      </BarChart>
    </ResponsiveContainer>
  )
}

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

  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-2xl font-semibold tracking-tight">{t('reports.title')}</h1>

      <Card>
        <CardHeader>
          <CardTitle className="text-base font-medium">{t('reports.billing.title')}</CardTitle>
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          <div className="flex flex-wrap items-end gap-3">
            <div className="w-40">
              <Field label={t('reports.dateFrom')}>
                <Input type="date" value={dateFrom} onChange={(e) => setDateFrom(e.target.value)} />
              </Field>
            </div>
            <div className="w-40">
              <Field label={t('reports.dateTo')}>
                <Input type="date" value={dateTo} onChange={(e) => setDateTo(e.target.value)} />
              </Field>
            </div>
            <Button
              size="sm"
              onClick={() =>
                setAppliedRange({ from: dateFrom || undefined, to: dateTo || undefined })
              }
            >
              {t('reports.apply')}
            </Button>
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

          {billingLoading && <Skeleton className="h-24 w-full" />}
          {billing && (
            <>
              <p className="text-xs text-muted-foreground">
                {formatDate(billing.dateFrom, lang)} – {formatDate(billing.dateTo, lang)}
              </p>
              <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-6">
                <MoneyStatCard
                  label={t('reports.billing.income')}
                  amount={billing.income}
                  lang={lang}
                />
                <MoneyStatCard
                  label={t('reports.billing.expenses')}
                  amount={billing.expenses}
                  lang={lang}
                />
                <MoneyStatCard
                  label={t('reports.billing.netResult')}
                  amount={billing.netResult}
                  lang={lang}
                />
                <MoneyStatCard
                  label={t('reports.billing.pendingInvoices')}
                  amount={billing.pendingInvoicesAmount}
                  lang={lang}
                />
                <MoneyStatCard
                  label={t('reports.billing.overdueInvoices')}
                  amount={billing.overdueInvoicesAmount}
                  lang={lang}
                />
                <MoneyStatCard
                  label={t('reports.billing.pendingPayroll')}
                  amount={billing.pendingPayrollAmount}
                  lang={lang}
                />
              </div>
            </>
          )}
        </CardContent>
      </Card>

      <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle className="text-base font-medium">{t('reports.students.title')}</CardTitle>
          </CardHeader>
          <CardContent className="flex flex-col gap-4">
            {studentsLoading && <Skeleton className="h-32 w-full" />}
            {students && (
              <>
                <StatCard label={t('reports.students.total')} value={toNum(students.total)} />
                <div>
                  <p className="mb-2 text-sm text-muted-foreground">
                    {t('reports.students.byStatus')}
                  </p>
                  <StatusChart data={students.byStatus} category="studentStatus" />
                </div>
              </>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="text-base font-medium">{t('reports.teachers.title')}</CardTitle>
          </CardHeader>
          <CardContent className="flex flex-col gap-4">
            {teachersLoading && <Skeleton className="h-32 w-full" />}
            {teachers && (
              <>
                <StatCard label={t('reports.teachers.total')} value={toNum(teachers.total)} />
                <div>
                  <p className="mb-2 text-sm text-muted-foreground">
                    {t('reports.teachers.byStatus')}
                  </p>
                  <StatusChart data={teachers.byStatus} category="teacherStatus" />
                </div>
                <div className="flex flex-col gap-2">
                  <p className="text-sm text-muted-foreground">
                    {t('reports.teachers.salaryOverview.title')}
                  </p>
                  <div className="grid grid-cols-2 gap-3">
                    <StatCard
                      label={t('reports.teachers.salaryOverview.teachersWithRate')}
                      value={toNum(teachers.salaryOverview.teachersWithRate)}
                    />
                    <MoneyStatCard
                      label={t('reports.teachers.salaryOverview.totalBaseSalary')}
                      amount={teachers.salaryOverview.totalBaseSalary}
                      lang={lang}
                    />
                    <MoneyStatCard
                      label={t('reports.teachers.salaryOverview.averageBaseSalary')}
                      amount={teachers.salaryOverview.averageBaseSalary}
                      lang={lang}
                    />
                    <MoneyStatCard
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
          <CardTitle className="text-base font-medium">{t('reports.enrollments.title')}</CardTitle>
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          {enrollmentsLoading && <Skeleton className="h-32 w-full" />}
          {enrollments && (
            <>
              <StatCard label={t('reports.enrollments.total')} value={toNum(enrollments.total)} />
              <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
                <div>
                  <p className="mb-2 text-sm text-muted-foreground">
                    {t('reports.enrollments.byStatus')}
                  </p>
                  <StatusChart data={enrollments.byStatus} category="enrollmentStatus" />
                </div>
                <div>
                  <p className="mb-2 text-sm text-muted-foreground">
                    {t('reports.enrollments.byCourse')}
                  </p>
                  {byCourseData.length === 0 ? (
                    <p className="text-sm text-muted-foreground">—</p>
                  ) : (
                    <ResponsiveContainer
                      width="100%"
                      height={Math.max(120, byCourseData.length * 32)}
                    >
                      <BarChart
                        data={byCourseData}
                        layout="vertical"
                        margin={{ left: 8, right: 16 }}
                      >
                        <CartesianGrid horizontal={false} stroke="var(--border)" />
                        <XAxis
                          type="number"
                          allowDecimals={false}
                          tick={{ fill: 'var(--muted-foreground)', fontSize: 12 }}
                        />
                        <YAxis
                          type="category"
                          dataKey="name"
                          width={140}
                          tick={{ fill: 'var(--muted-foreground)', fontSize: 12 }}
                        />
                        <Tooltip
                          contentStyle={{
                            background: 'var(--popover)',
                            border: '1px solid var(--border)',
                            borderRadius: 8,
                            color: 'var(--popover-foreground)',
                            fontSize: 12,
                          }}
                        />
                        <Bar
                          dataKey="value"
                          fill="var(--primary)"
                          radius={[0, 4, 4, 0]}
                          animationDuration={500}
                          animationEasing="ease-out"
                          activeBar={{ fillOpacity: 0.75 }}
                        />
                      </BarChart>
                    </ResponsiveContainer>
                  )}
                </div>
              </div>
            </>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
