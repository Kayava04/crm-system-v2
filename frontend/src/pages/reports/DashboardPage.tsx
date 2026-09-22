import { useState, type ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { useQuery } from '@tanstack/react-query'
import {
  Bar,
  BarChart,
  CartesianGrid,
  Cell,
  Pie,
  PieChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'
import {
  Wallet,
  TrendingDown,
  Scale,
  FileClock,
  AlertTriangle,
  Landmark,
  type LucideIcon,
} from 'lucide-react'
import {
  getBillingSummary,
  getEnrollmentsSummary,
  getStudentsSummary,
  getTeachersSummary,
} from '@/features/reports/api'
import { enumLabel, type EnumCategory } from '@/lib/enumLabels'
import { toNum, formatCurrencyParts, formatDate, cn } from '@/lib/utils'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import { Field } from '@/components/shared/Field'

// Categorical chart series — the dataviz skill's validated default palette
// (see index.css), mapped to app tokens so the dashboard never drifts from
// the rest of the app's colors.
const CHART_COLORS = [
  'var(--chart-1)',
  'var(--chart-2)',
  'var(--chart-3)',
  'var(--chart-4)',
  'var(--chart-5)',
]

// Every status a category can have, in a FIXED order — a status keeps the
// same color across renders and filters ("color follows the entity, never
// its rank"), instead of being colored by its position among only the
// statuses that happen to have a nonzero count this time.
const STUDENT_STATUS_ORDER = ['Active', 'Suspended', 'Graduated', 'Withdrawn'] as const
const TEACHER_STATUS_ORDER = ['Probation', 'Employed', 'OnLeave', 'Resigned', 'Dismissed'] as const
const ENROLLMENT_STATUS_ORDER = ['Draft', 'Active', 'Suspended', 'Completed', 'Terminated'] as const

function colorForStatus(status: string, order: readonly string[]): string {
  const idx = order.indexOf(status)
  return CHART_COLORS[(idx >= 0 ? idx : 0) % CHART_COLORS.length]
}

type Tone = 'primary' | 'success' | 'warning' | 'destructive' | 'muted'

const TONE_CLASSES: Record<Tone, string> = {
  primary: 'bg-primary/10 text-primary',
  success: 'bg-success/15 text-success',
  warning: 'bg-warning/25 text-warning-foreground',
  destructive: 'bg-destructive/10 text-destructive',
  muted: 'bg-muted text-muted-foreground',
}

function StatTile({
  icon: Icon,
  label,
  value,
  unit,
  tone = 'muted',
}: {
  icon?: LucideIcon
  label: string
  value: ReactNode
  /** A smaller, muted suffix next to the value — e.g. the currency symbol,
   * kept visually secondary to the number itself. */
  unit?: string
  tone?: Tone
}) {
  return (
    <Card className="animate-in fade-in-0 slide-in-from-bottom-1 duration-500">
      <CardContent className="flex h-full flex-col gap-3 py-4">
        <div className="flex items-start justify-between gap-2">
          <span className="line-clamp-2 min-h-10 text-sm leading-tight text-muted-foreground">
            {label}
          </span>
          {Icon && (
            <span
              className={cn(
                'flex size-8 shrink-0 items-center justify-center rounded-lg',
                TONE_CLASSES[tone],
              )}
            >
              <Icon className="size-4" />
            </span>
          )}
        </div>
        <span className="flex items-baseline gap-1">
          <span className="text-2xl font-semibold tracking-tight">{value}</span>
          {unit && <span className="text-sm font-normal text-muted-foreground">{unit}</span>}
        </span>
      </CardContent>
    </Card>
  )
}

/** Renders a money StatTile with the currency unit visually smaller than the
 * number, instead of one same-size formatted string. */
function MoneyStatTile({
  label,
  amount,
  lang,
  icon,
  tone,
}: {
  label: string
  amount: number | string
  lang: string
  icon?: LucideIcon
  tone?: Tone
}) {
  const { value, unit } = formatCurrencyParts(amount, lang)
  return <StatTile label={label} value={value} unit={unit} icon={icon} tone={tone} />
}

function tooltipStyle() {
  return {
    background: 'var(--popover)',
    border: '1px solid var(--border)',
    borderRadius: 8,
    color: 'var(--popover-foreground)',
    fontSize: 12,
  }
}

/** A donut of a status breakdown, with the total in the center and a
 * side legend carrying the value for every slice — identity never rides on
 * color alone, and nothing is labeled only on hover. */
function StatusDonut({
  data,
  category,
  order,
}: {
  data: Record<string, number | string>
  category: EnumCategory
  order: readonly string[]
}) {
  const { t } = useTranslation()
  const chartData = order
    .map((status) => ({
      status,
      name: enumLabel(t, category, status),
      value: toNum(data[status] ?? 0),
      fill: colorForStatus(status, order),
    }))
    .filter((d) => d.value > 0)
  const total = chartData.reduce((sum, d) => sum + d.value, 0)

  if (total === 0) {
    return <p className="text-sm text-muted-foreground">—</p>
  }

  return (
    <div className="flex flex-col items-center gap-4 sm:flex-row">
      <div className="relative size-[168px] shrink-0">
        <ResponsiveContainer width="100%" height="100%">
          <PieChart>
            <Pie
              data={chartData}
              dataKey="value"
              nameKey="name"
              innerRadius="64%"
              outerRadius="100%"
              paddingAngle={chartData.length > 1 ? 3 : 0}
              cornerRadius={4}
              stroke="none"
              isAnimationActive
              animationDuration={600}
              animationEasing="ease-out"
            >
              {chartData.map((d) => (
                <Cell key={d.status} fill={d.fill} />
              ))}
            </Pie>
            <Tooltip contentStyle={tooltipStyle()} />
          </PieChart>
        </ResponsiveContainer>
        <div className="pointer-events-none absolute inset-0 flex flex-col items-center justify-center">
          <span className="text-2xl font-semibold tracking-tight tabular-nums">{total}</span>
        </div>
      </div>
      <div className="flex w-full flex-1 flex-col gap-1.5">
        {chartData.map((d) => (
          <div key={d.status} className="flex items-center justify-between gap-3 text-sm">
            <span className="flex min-w-0 items-center gap-2 text-muted-foreground">
              <span
                className="size-2.5 shrink-0 rounded-full"
                style={{ background: d.fill }}
                aria-hidden
              />
              <span className="truncate">{d.name}</span>
            </span>
            <span className="shrink-0 font-medium tabular-nums">{d.value}</span>
          </div>
        ))}
      </div>
    </div>
  )
}

/** The one magnitude-ranking chart on the dashboard — a single series, so it
 * stays a single hue (--primary) rather than the categorical palette, per
 * the "sequential/magnitude = one hue" rule. */
function RankingBarChart({ data }: { data: { name: string; value: number }[] }) {
  if (data.length === 0) {
    return <p className="text-sm text-muted-foreground">—</p>
  }
  return (
    <ResponsiveContainer width="100%" height={Math.max(120, data.length * 34)}>
      <BarChart data={data} layout="vertical" margin={{ left: 8, right: 24 }} barCategoryGap={8}>
        <CartesianGrid horizontal={false} stroke="var(--border)" strokeOpacity={0.6} />
        <XAxis
          type="number"
          allowDecimals={false}
          tick={{ fill: 'var(--muted-foreground)', fontSize: 12 }}
          axisLine={{ stroke: 'var(--border)' }}
          tickLine={false}
        />
        <YAxis
          type="category"
          dataKey="name"
          width={140}
          tick={{ fill: 'var(--muted-foreground)', fontSize: 12 }}
          axisLine={false}
          tickLine={false}
        />
        <Tooltip contentStyle={tooltipStyle()} cursor={{ fill: 'var(--muted)', opacity: 0.5 }} />
        <Bar
          dataKey="value"
          fill="var(--primary)"
          radius={[0, 4, 4, 0]}
          maxBarSize={18}
          isAnimationActive
          animationDuration={600}
          animationEasing="ease-out"
          activeBar={{ fillOpacity: 0.8 }}
        />
      </BarChart>
    </ResponsiveContainer>
  )
}

/** Income vs. expenses for the selected period — two bars, colored with the
 * app's reserved status hues (income = success, expenses = destructive)
 * rather than the categorical chart palette, since these are states/signs,
 * not arbitrary series. */
function IncomeExpenseBars({
  income,
  expenses,
  lang,
}: {
  income: number | string
  expenses: number | string
  lang: string
}) {
  const { t } = useTranslation()
  const rows = [
    {
      key: 'income',
      name: t('reports.billing.income'),
      value: toNum(income),
      color: 'var(--success)',
    },
    {
      key: 'expenses',
      name: t('reports.billing.expenses'),
      value: toNum(expenses),
      color: 'var(--destructive)',
    },
  ]
  const max = Math.max(1, ...rows.map((r) => r.value))

  // Plain CSS bars instead of a Recharts SVG chart: with only two rows and
  // short mixed-length labels ("Доходи" / "Витрати"), Recharts' per-row tick
  // measurement produced a baseline that drifted a couple of pixels between
  // rows, so labels and bars never quite lined up. Flexbox rows with fixed
  // label/value columns guarantee both rows share the exact same baseline.
  return (
    <div className="flex h-[92px] flex-col justify-center gap-3">
      {rows.map((row) => {
        const { value: amountValue, unit } = formatCurrencyParts(row.value, lang)
        const pct = row.value > 0 ? Math.max(4, (row.value / max) * 100) : 0
        return (
          <div key={row.key} className="flex items-center gap-3">
            <span className="w-[72px] shrink-0 truncate text-xs text-muted-foreground">
              {row.name}
            </span>
            <div className="h-5 flex-1 overflow-hidden rounded-sm bg-muted/40">
              <div
                className="h-full rounded-r-[4px] transition-[width] duration-500 ease-out"
                style={{ width: `${pct}%`, background: row.color }}
                title={`${row.name}: ${amountValue}${unit ? ` ${unit}` : ''}`}
              />
            </div>
            <span className="w-[84px] shrink-0 text-right text-xs font-medium tabular-nums">
              {amountValue}
              {unit && <span className="ml-0.5 font-normal text-muted-foreground">{unit}</span>}
            </span>
          </div>
        )
      })}
    </div>
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

  const netPositive = billing ? toNum(billing.netResult) >= 0 : true

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

                <div className="flex w-full flex-col justify-center rounded-lg border border-border bg-muted/30 p-3 lg:w-64">
                  <p className="mb-1 text-base font-medium">
                    {t('reports.billing.incomeVsExpenses')}
                  </p>
                  <IncomeExpenseBars
                    income={billing.income}
                    expenses={billing.expenses}
                    lang={lang}
                  />
                </div>
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
                <StatTile label={t('reports.students.total')} value={toNum(students.total)} />
                <div>
                  <p className="mb-2 text-sm text-muted-foreground">
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
            <CardTitle className="text-base font-medium">{t('reports.teachers.title')}</CardTitle>
          </CardHeader>
          <CardContent className="flex flex-col gap-4">
            {teachersLoading && <Skeleton className="h-32 w-full" />}
            {teachers && (
              <>
                <StatTile label={t('reports.teachers.total')} value={toNum(teachers.total)} />
                <div>
                  <p className="mb-2 text-sm text-muted-foreground">
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
                  <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
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
          <CardTitle className="text-base font-medium">{t('reports.enrollments.title')}</CardTitle>
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          {enrollmentsLoading && <Skeleton className="h-32 w-full" />}
          {enrollments && (
            <>
              <StatTile label={t('reports.enrollments.total')} value={toNum(enrollments.total)} />
              <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
                <div>
                  <p className="mb-2 text-sm text-muted-foreground">
                    {t('reports.enrollments.byStatus')}
                  </p>
                  <StatusDonut
                    data={enrollments.byStatus}
                    category="enrollmentStatus"
                    order={ENROLLMENT_STATUS_ORDER}
                  />
                </div>
                <div>
                  <p className="mb-2 text-sm text-muted-foreground">
                    {t('reports.enrollments.byCourse')}
                  </p>
                  <RankingBarChart data={byCourseData} />
                </div>
              </div>
            </>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
