import { useId, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useQuery } from '@tanstack/react-query'
import { addDays, format, getISODay, startOfDay } from 'date-fns'
import { Bar, BarChart, Cell, LabelList, ResponsiveContainer, Tooltip, XAxis } from 'recharts'
import { getSchedules, type ScheduleListItem } from '@/features/scheduling/api'
import { toNum } from '@/lib/utils'
import { Skeleton } from '@/components/ui/skeleton'
import { ChartTooltip } from './ChartTooltip'

const WINDOW_DAYS = 28
const MAX_PAGES = 20

async function fetchLessons(dateFrom: string, dateTo: string) {
  const items: ScheduleListItem[] = []
  for (let page = 1; page <= MAX_PAGES; page++) {
    const res = await getSchedules({ dateFrom, dateTo, page, pageSize: 100 })
    items.push(...res.items)
    if (page >= toNum(res.totalPages)) break
  }
  return items
}

export function LessonsByWeekday({ lang }: { lang: string }) {
  const { t } = useTranslation()
  const locale = lang === 'en' ? 'en-US' : 'uk-UA'
  const gradientId = useId().replace(/:/g, '')
  const [active, setActive] = useState<number | null>(null)

  const to = startOfDay(new Date())
  const from = addDays(to, -(WINDOW_DAYS - 1))
  const range = { from: format(from, 'yyyy-MM-dd'), to: format(to, 'yyyy-MM-dd') }

  const { data: lessons, isLoading } = useQuery({
    queryKey: ['reports', 'lessons-by-weekday', range],
    queryFn: () => fetchLessons(range.from, range.to),
    staleTime: 5 * 60_000,
  })

  const shortDay = new Intl.DateTimeFormat(locale, { weekday: 'short' })
  const longDay = new Intl.DateTimeFormat(locale, { weekday: 'long' })
  const data = Array.from({ length: 7 }, (_, i) => {
    const sample = new Date(2024, 0, 1 + i)
    return {
      day: i + 1,
      short: shortDay.format(sample),
      long: longDay.format(sample),
      count: 0,
      track: 0,
    }
  })
  for (const lesson of lessons ?? []) {
    if (lesson.status === 'Cancelled') continue
    data[getISODay(new Date(lesson.scheduledDate)) - 1].count++
  }

  const total = data.reduce((sum, d) => sum + d.count, 0)
  const peak = data.reduce((best, d) => (d.count > best.count ? d : best), data[0])
  for (const d of data) d.track = Math.max(1, peak.count)

  return (
    <div className="flex h-full flex-col gap-4">
      <p className="text-xs text-muted-foreground">
        {total > 0 ? (
          <>
            {t('reports.lessons.peak')}:{' '}
            <span className="font-medium text-foreground first-letter:uppercase">{peak.long}</span>{' '}
            · {t('reports.lessons.total', { count: total })}
          </>
        ) : (
          t('reports.lessons.empty')
        )}
      </p>

      {isLoading ? (
        <Skeleton className="h-64 w-full" />
      ) : (
        <div className="h-64 animate-fade-in">
          <ResponsiveContainer width="100%" height="100%">
            <BarChart
              data={data}
              margin={{ top: 20, right: 0, left: 0, bottom: 0 }}
              barCategoryGap="18%"
              onMouseMove={(state) =>
                setActive(
                  state.activeTooltipIndex != null ? Number(state.activeTooltipIndex) : null,
                )
              }
              onMouseLeave={() => setActive(null)}
            >
              <defs>
                <linearGradient id={gradientId} x1="0" y1="0" x2="0" y2="1">
                  <stop offset="0%" stopColor="var(--chart-gradient-to)" />
                  <stop offset="100%" stopColor="var(--chart-gradient-from)" />
                </linearGradient>
              </defs>
              <XAxis xAxisId="track" dataKey="short" hide />
              <XAxis
                dataKey="short"
                tick={{ fill: 'var(--muted-foreground)', fontSize: 12 }}
                axisLine={false}
                tickLine={false}
                tickMargin={8}
              />
              <Tooltip
                cursor={false}
                content={({ active: isActive, payload }) => {
                  const d = payload?.[0]?.payload as (typeof data)[number] | undefined
                  if (!isActive || !d) return null
                  return (
                    <ChartTooltip
                      title={<span className="capitalize">{d.long}</span>}
                      rows={[{ key: 'count', name: t('reports.lessons.lessons'), value: d.count }]}
                    />
                  )
                }}
              />
              <Bar
                xAxisId="track"
                dataKey="track"
                fill="var(--foreground)"
                fillOpacity={0.05}
                radius={8}
                isAnimationActive={false}
              />
              <Bar
                dataKey="count"
                fill={`url(#${gradientId})`}
                radius={8}
                animationDuration={800}
                animationEasing="ease-out"
              >
                {data.map((d, i) => (
                  <Cell
                    key={d.day}
                    fillOpacity={active === null || active === i ? 1 : 0.4}
                    style={{ transition: 'fill-opacity var(--duration-fast)' }}
                  />
                ))}
                <LabelList
                  dataKey="count"
                  position="top"
                  offset={6}
                  className="fill-muted-foreground text-xs tabular-nums"
                />
              </Bar>
            </BarChart>
          </ResponsiveContainer>
        </div>
      )}
    </div>
  )
}
