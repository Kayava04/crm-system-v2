import { useState } from 'react'
import { cn } from '@/lib/utils'

/** The one magnitude ranking on the dashboard — a single series, so it stays
 * one hue (a brand gradient) rather than the categorical palette. Plain markup
 * instead of an SVG chart: long course names stay readable at any width, and
 * every value is printed rather than hidden behind a tooltip. */
export function RankingBars({ data }: { data: { name: string; value: number }[] }) {
  const [active, setActive] = useState<number | null>(null)

  if (data.length === 0) {
    return <p className="text-sm text-muted-foreground">—</p>
  }

  const max = Math.max(1, ...data.map((d) => d.value))

  return (
    <ol className="grid grid-cols-1 gap-x-8 gap-y-1 md:grid-cols-2">
      {data.map((d, i) => (
        <li
          key={d.name}
          onMouseEnter={() => setActive(i)}
          onMouseLeave={() => setActive(null)}
          className={cn(
            'grid grid-cols-[1.75rem_1fr] items-center gap-x-2 rounded-lg px-2 py-2 transition-[background-color,opacity]',
            active === i && 'bg-muted',
            active !== null && active !== i && 'opacity-55',
          )}
        >
          <span className="row-span-2 text-xs font-medium text-muted-foreground tabular-nums">
            {String(i + 1).padStart(2, '0')}
          </span>
          <div className="flex items-baseline justify-between gap-3 text-sm">
            <span className="truncate" title={d.name}>
              {d.name}
            </span>
            <span className="shrink-0 font-medium tabular-nums">{d.value}</span>
          </div>
          <div className="mt-1.5 h-2 overflow-hidden rounded-full bg-foreground/6">
            <div
              className="h-full origin-left animate-grow-x rounded-full bg-linear-to-r from-(--chart-gradient-from) to-(--chart-gradient-to)"
              style={{ transform: `scaleX(${d.value / max})`, animationDelay: `${i * 50}ms` }}
            />
          </div>
        </li>
      ))}
    </ol>
  )
}
