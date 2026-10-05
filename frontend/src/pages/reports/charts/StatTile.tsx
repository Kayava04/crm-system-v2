import type { ReactNode } from 'react'
import type { LucideIcon } from 'lucide-react'
import { cn, formatCurrencyParts } from '@/lib/utils'

export type Tone = 'primary' | 'success' | 'warning' | 'destructive' | 'muted'

const TONE_CLASSES: Record<Tone, string> = {
  primary: 'bg-primary/10 text-primary',
  success: 'bg-success/15 text-success',
  warning: 'bg-warning/25 text-warning-foreground',
  destructive: 'bg-destructive/10 text-destructive',
  muted: 'bg-foreground/6 text-muted-foreground',
}

export function StatTile({
  icon: Icon,
  label,
  value,
  unit,
  tone = 'muted',
}: {
  icon?: LucideIcon
  label: string
  value: ReactNode
  unit?: string
  tone?: Tone
}) {
  return (
    <div className="flex min-w-0 animate-rise-in flex-col gap-3 rounded-xl bg-muted/60 p-3.5 sm:p-4">
      <div className="flex items-start justify-between gap-2">
        <span className="line-clamp-3 min-h-10 min-w-0 flex-1 sm:line-clamp-2 text-sm leading-tight break-words text-muted-foreground">
          {label}
        </span>
        {Icon && (
          <span
            className={cn(
              'flex size-7 shrink-0 items-center justify-center rounded-lg sm:size-8',
              TONE_CLASSES[tone],
            )}
          >
            <Icon className="size-4" />
          </span>
        )}
      </div>
      <span className="flex flex-wrap items-baseline gap-1">
        <span className="text-lg font-semibold tabular-nums break-words sm:text-2xl">{value}</span>
        {unit && <span className="text-sm text-muted-foreground">{unit}</span>}
      </span>
    </div>
  )
}

export function MoneyStatTile({
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
