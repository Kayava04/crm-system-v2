import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Pie, PieChart, ResponsiveContainer, Sector } from 'recharts'
import { enumLabel, type EnumCategory } from '@/lib/enumLabels'
import { cn, toNum } from '@/lib/utils'
import { colorForStatus } from './palette'

/** A donut of a status breakdown. The center reads out the hovered slice (or
 * the total), and the legend carries every value and share — identity never
 * rides on color alone, and nothing is labeled only on hover. */
export function StatusDonut({
  data,
  category,
  order,
}: {
  data: Record<string, number | string>
  category: EnumCategory
  order: readonly string[]
}) {
  const { t } = useTranslation()
  const [active, setActive] = useState<number | null>(null)

  const slices = order
    .map((status) => ({
      status,
      name: enumLabel(t, category, status),
      value: toNum(data[status] ?? 0),
      fill: colorForStatus(status, order),
    }))
    .filter((d) => d.value > 0)
  const total = slices.reduce((sum, d) => sum + d.value, 0)

  if (total === 0) {
    return <p className="text-sm text-muted-foreground">—</p>
  }

  const current = active !== null ? slices[active] : null
  const share = (value: number) => `${Math.round((value / total) * 100)}%`

  return (
    <div className="@container">
      <div className="flex flex-col items-center gap-6 @md:flex-row">
        <div className="relative size-44 shrink-0">
          <ResponsiveContainer width="100%" height="100%">
            <PieChart>
              <Pie
                data={slices}
                dataKey="value"
                nameKey="name"
                innerRadius="72%"
                outerRadius="94%"
                startAngle={90}
                endAngle={-270}
                paddingAngle={slices.length > 1 ? 2.5 : 0}
                cornerRadius={6}
                stroke="none"
                rootTabIndex={-1}
                animationDuration={700}
                animationEasing="ease-out"
                onMouseEnter={(_, index) => setActive(index)}
                onMouseLeave={() => setActive(null)}
                shape={(props, index) => (
                  // Sector drops `style`, so the hover transform lives on a wrapper.
                  <g
                    style={{
                      transformOrigin: `${props.cx}px ${props.cy}px`,
                      transform: index === active ? 'scale(1.05)' : undefined,
                      opacity: active === null || index === active ? 1 : 0.35,
                      transition:
                        'transform var(--duration-base) var(--ease-out-soft), opacity var(--duration-fast)',
                    }}
                  >
                    <Sector {...props} />
                  </g>
                )}
              />
            </PieChart>
          </ResponsiveContainer>
          <div className="pointer-events-none absolute inset-0 flex flex-col items-center justify-center text-center">
            <span className="text-3xl font-semibold tabular-nums">
              {current ? current.value : total}
            </span>
            <span className="max-w-24 truncate text-xs text-muted-foreground">
              {current ? current.name : t('reports.total')}
            </span>
          </div>
        </div>

        <ul className="flex w-full flex-1 flex-col gap-0.5">
          {slices.map((d, i) => (
            <li
              key={d.status}
              onMouseEnter={() => setActive(i)}
              onMouseLeave={() => setActive(null)}
              className={cn(
                'rounded-lg px-2.5 py-2 transition-[background-color,opacity]',
                active === i && 'bg-muted',
                active !== null && active !== i && 'opacity-50',
              )}
            >
              <div className="flex items-center justify-between gap-3 text-sm">
                <span className="flex min-w-0 items-center gap-2">
                  <span
                    className="size-2 shrink-0 rounded-full"
                    style={{ background: d.fill }}
                    aria-hidden
                  />
                  <span className="truncate">{d.name}</span>
                </span>
                <span className="flex shrink-0 items-baseline gap-2 tabular-nums">
                  <span className="font-medium">{d.value}</span>
                  <span className="w-9 text-right text-xs text-muted-foreground">
                    {share(d.value)}
                  </span>
                </span>
              </div>
              <div className="mt-1.5 h-1 overflow-hidden rounded-full bg-foreground/6">
                <div
                  className="h-full origin-left animate-grow-x rounded-full"
                  style={{
                    transform: `scaleX(${d.value / total})`,
                    background: d.fill,
                    animationDelay: `${i * 60}ms`,
                  }}
                />
              </div>
            </li>
          ))}
        </ul>
      </div>
    </div>
  )
}
