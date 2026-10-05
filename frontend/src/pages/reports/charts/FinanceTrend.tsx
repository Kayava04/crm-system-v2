import { useId, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useQueries } from '@tanstack/react-query'
import { addMonths, endOfMonth, format, startOfMonth } from 'date-fns'
import {
  Area,
  AreaChart,
  CartesianGrid,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'
import { getBillingSummary } from '@/features/reports/api'
import { formatCurrencyParts, toNum } from '@/lib/utils'
import { Skeleton } from '@/components/ui/skeleton'
import { ChartTooltip } from './ChartTooltip'
import { SegmentedControl } from './SegmentedControl'

const SERIES = [
  { key: 'income', color: 'var(--chart-3)' },
  { key: 'expenses', color: 'var(--chart-2)' },
] as const

type Point = { month: Date; income: number; expenses: number }

function capitalize(s: string) {
  return s.charAt(0).toUpperCase() + s.slice(1)
}

export function FinanceTrend({ lang }: { lang: string }) {
  const { t } = useTranslation()
  const locale = lang === 'en' ? 'en-US' : 'uk-UA'
  const gradientId = useId().replace(/:/g, '')
  const [span, setSpan] = useState<6 | 12>(6)

  const thisMonth = startOfMonth(new Date())
  const months = Array.from({ length: span }, (_, i) => addMonths(thisMonth, i - span + 1))

  const results = useQueries({
    queries: months.map((m) => {
      const range = { from: format(m, 'yyyy-MM-dd'), to: format(endOfMonth(m), 'yyyy-MM-dd') }
      return {
        queryKey: ['reports', 'billing', range],
        queryFn: () => getBillingSummary(range.from, range.to),
        staleTime: 5 * 60_000,
      }
    }),
  })

  const isLoading = results.some((r) => r.isLoading)
  const data: Point[] = months.map((month, i) => ({
    month,
    income: toNum(results[i].data?.income ?? 0),
    expenses: toNum(results[i].data?.expenses ?? 0),
  }))

  const shortMonth = new Intl.DateTimeFormat(locale, { month: 'short' })
  const longMonth = new Intl.DateTimeFormat(locale, { month: 'long', year: 'numeric' })
  const compact = new Intl.NumberFormat(locale, { notation: 'compact', maximumFractionDigits: 1 })
  const money = (n: number) => {
    const { value, unit } = formatCurrencyParts(n, lang)
    return `${value} ${unit}`
  }

  return (
    <div className="flex h-full flex-col gap-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="flex items-center gap-4 text-xs text-muted-foreground">
          {SERIES.map((s) => (
            <span key={s.key} className="flex items-center gap-1.5">
              <span
                className="h-0.5 w-3 rounded-full"
                style={{ background: s.color }}
                aria-hidden
              />
              {t(`reports.billing.${s.key}`)}
            </span>
          ))}
        </div>
        <SegmentedControl
          label={t('reports.trend.span')}
          value={span}
          onChange={setSpan}
          options={[
            { value: 6, label: t('reports.trend.months', { count: 6 }) },
            { value: 12, label: t('reports.trend.months', { count: 12 }) },
          ]}
        />
      </div>

      {isLoading ? (
        <Skeleton className="h-64 w-full" />
      ) : (
        <div className="h-64 animate-fade-in">
          <ResponsiveContainer width="100%" height="100%">
            <AreaChart data={data} margin={{ top: 8, right: 8, left: 0, bottom: 0 }}>
              <defs>
                {SERIES.map((s) => (
                  <linearGradient
                    key={s.key}
                    id={`${gradientId}-${s.key}`}
                    x1="0"
                    y1="0"
                    x2="0"
                    y2="1"
                  >
                    <stop offset="0%" stopColor={s.color} stopOpacity={0.32} />
                    <stop offset="100%" stopColor={s.color} stopOpacity={0} />
                  </linearGradient>
                ))}
              </defs>
              <CartesianGrid
                vertical={false}
                stroke="var(--foreground)"
                strokeOpacity={0.07}
                strokeDasharray="4 6"
              />
              <XAxis
                dataKey="month"
                tickFormatter={(m: Date) => capitalize(shortMonth.format(m))}
                tick={{ fill: 'var(--muted-foreground)', fontSize: 12 }}
                axisLine={false}
                tickLine={false}
                tickMargin={10}
              />
              <YAxis
                tickFormatter={(n: number) => compact.format(n)}
                tick={{ fill: 'var(--muted-foreground)', fontSize: 12 }}
                axisLine={false}
                tickLine={false}
                width={60}
              />
              <Tooltip
                cursor={{ stroke: 'var(--foreground)', strokeOpacity: 0.2, strokeDasharray: '4 4' }}
                content={({ active, payload }) => {
                  const point = payload?.[0]?.payload as Point | undefined
                  if (!active || !point) return null
                  return (
                    <ChartTooltip
                      title={capitalize(longMonth.format(point.month))}
                      rows={[
                        ...SERIES.map((s) => ({
                          key: s.key,
                          name: t(`reports.billing.${s.key}`),
                          value: money(point[s.key]),
                          color: s.color,
                        })),
                        {
                          key: 'net',
                          name: t('reports.billing.netResult'),
                          value: money(point.income - point.expenses),
                        },
                      ]}
                    />
                  )
                }}
              />
              {SERIES.map((s) => (
                <Area
                  key={s.key}
                  type="monotone"
                  dataKey={s.key}
                  stroke={s.color}
                  strokeWidth={2.5}
                  fill={`url(#${gradientId}-${s.key})`}
                  dot={false}
                  activeDot={{ r: 5, strokeWidth: 3, stroke: 'var(--card)', fill: s.color }}
                  animationDuration={900}
                  animationEasing="ease-out"
                />
              ))}
            </AreaChart>
          </ResponsiveContainer>
        </div>
      )}
    </div>
  )
}
