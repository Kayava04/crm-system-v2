import type { ReactNode } from 'react'

export interface TooltipRow {
  key: string
  name: string
  value: ReactNode
  color?: string
}

/** Frosted tooltip body shared by the dashboard's Recharts charts. */
export function ChartTooltip({ title, rows }: { title: ReactNode; rows: TooltipRow[] }) {
  return (
    <div className="glass min-w-40 animate-fade-in rounded-xl px-3 py-2.5 text-xs [--glass-bg:var(--glass-bg-strong)]">
      <p className="mb-1.5 font-medium text-muted-foreground">{title}</p>
      <div className="flex flex-col gap-1">
        {rows.map((row) => (
          <div key={row.key} className="flex items-center justify-between gap-4">
            <span className="flex items-center gap-1.5">
              {row.color && (
                <span
                  className="size-2 rounded-full"
                  style={{ background: row.color }}
                  aria-hidden
                />
              )}
              {row.name}
            </span>
            <span className="font-semibold tabular-nums">{row.value}</span>
          </div>
        ))}
      </div>
    </div>
  )
}
