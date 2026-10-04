import * as React from 'react'
import { useTranslation } from 'react-i18next'
import {
  addDays,
  addMonths,
  endOfMonth,
  endOfWeek,
  format,
  isBefore,
  isSameDay,
  isValid,
  parseISO,
  startOfDay,
  startOfMonth,
  startOfWeek,
} from 'date-fns'
import { CalendarDays, ChevronDown } from 'lucide-react'
import { cn } from '@/lib/utils'
import { Button } from './button'
import { Calendar, type DateRange } from './calendar'
import { fieldClasses } from './field-styles'
import { Popover, PopoverContent, PopoverTrigger } from './popover'

/** ISO `yyyy-MM-dd` strings — the same shape the form state and API use. */
export interface DateRangeValue {
  from: string
  to: string
}

type PresetKey = 'today' | 'thisWeek' | 'thisMonth' | 'last7Days' | 'last30Days' | 'lastMonth'

function presetRange(key: PresetKey): DateRange {
  const today = startOfDay(new Date())
  switch (key) {
    case 'today':
      return { from: today, to: today }
    case 'thisWeek':
      return {
        from: startOfWeek(today, { weekStartsOn: 1 }),
        to: endOfWeek(today, { weekStartsOn: 1 }),
      }
    case 'thisMonth':
      return { from: startOfMonth(today), to: endOfMonth(today) }
    case 'last7Days':
      return { from: addDays(today, -6), to: today }
    case 'last30Days':
      return { from: addDays(today, -29), to: today }
    case 'lastMonth': {
      const prev = addMonths(today, -1)
      return { from: startOfMonth(prev), to: endOfMonth(prev) }
    }
  }
}

const PRESETS: PresetKey[] = [
  'today',
  'thisWeek',
  'thisMonth',
  'last7Days',
  'last30Days',
  'lastMonth',
]

function parse(value: string) {
  if (!value) return null
  const d = parseISO(value)
  return isValid(d) ? d : null
}

function toIso(d: Date | null) {
  return d ? format(d, 'yyyy-MM-dd') : ''
}

function sameRange(a: DateRange, b: DateRange) {
  return (
    !!a.from && !!a.to && !!b.from && !!b.to && isSameDay(a.from, b.from) && isSameDay(a.to, b.to)
  )
}

/** One field for a from–to period: presets, a range calendar and an explicit
 * Apply, so the period only changes when the user confirms it. */
export function DateRangePicker({
  value,
  onChange,
  placeholder,
  className,
  id,
}: {
  value: DateRangeValue
  onChange: (value: DateRangeValue) => void
  placeholder?: string
  className?: string
  id?: string
}) {
  const { t, i18n } = useTranslation()
  const locale = i18n.language === 'en' ? 'en-US' : 'uk-UA'
  const [open, setOpen] = React.useState(false)
  const [draft, setDraft] = React.useState<DateRange>({ from: null, to: null })
  // Remounts the calendar so a preset also jumps it to the preset's month.
  const [calendarKey, setCalendarKey] = React.useState(0)

  const current: DateRange = { from: parse(value.from), to: parse(value.to) }

  const longFormat = new Intl.DateTimeFormat(locale, {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
  })
  const shortFormat = new Intl.DateTimeFormat(locale, {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  })

  function describe(range: DateRange, fmt: Intl.DateTimeFormat) {
    if (!range.from) return ''
    const to = range.to ?? range.from
    return isSameDay(range.from, to) ? fmt.format(range.from) : fmt.formatRange(range.from, to)
  }

  function pickDay(day: Date) {
    setDraft((prev) => {
      if (!prev.from || prev.to) return { from: day, to: null }
      return isBefore(day, prev.from) ? { from: day, to: prev.from } : { from: prev.from, to: day }
    })
  }

  function applyPreset(key: PresetKey) {
    setDraft(presetRange(key))
    setCalendarKey((k) => k + 1)
  }

  function apply() {
    // A single click means a one-day period rather than an unfinished one.
    const to = draft.to ?? draft.from
    onChange({ from: toIso(draft.from), to: toIso(to) })
    setOpen(false)
  }

  const label = describe(current, longFormat)

  return (
    <Popover
      open={open}
      onOpenChange={(next) => {
        if (next) {
          setDraft(current)
          setCalendarKey((k) => k + 1)
        }
        setOpen(next)
      }}
    >
      <PopoverTrigger asChild>
        <button
          id={id}
          type="button"
          className={cn(
            fieldClasses,
            'flex h-9 items-center gap-2 text-left data-[state=open]:border-ring data-[state=open]:ring-3 data-[state=open]:ring-ring/25',
            className,
          )}
        >
          <CalendarDays className="size-4 shrink-0 text-muted-foreground" />
          <span className={cn('min-w-0 flex-1 truncate', !label && 'text-muted-foreground')}>
            {label || placeholder || t('datePicker.pickRange')}
          </span>
          <ChevronDown className="size-4 shrink-0 text-muted-foreground" />
        </button>
      </PopoverTrigger>
      <PopoverContent align="start" className="w-auto">
        <div className="mb-3 grid grid-cols-3 gap-1.5">
          {PRESETS.map((key) => (
            <button
              key={key}
              type="button"
              onClick={() => applyPreset(key)}
              className={cn(
                'h-8 truncate rounded-lg border border-foreground/8 px-2 text-xs font-medium outline-none transition-[color,background-color,border-color,transform]',
                'hover:bg-foreground/6 focus-visible:ring-2 focus-visible:ring-ring active:scale-95',
                sameRange(draft, presetRange(key)) &&
                  'border-primary/30 bg-primary/10 text-primary hover:bg-primary/15',
              )}
            >
              {t(`datePicker.presets.${key}`)}
            </button>
          ))}
        </div>

        <div className="border-t border-foreground/8 pt-3">
          <Calendar key={calendarKey} range={draft} onSelect={pickDay} />
        </div>

        <div className="mt-3 flex flex-col gap-3 border-t border-foreground/8 pt-3">
          <p className="text-xs text-muted-foreground">
            {t('datePicker.range')}:{' '}
            <span className="font-medium text-foreground tabular-nums">
              {describe(draft, shortFormat) || '—'}
            </span>
          </p>
          <div className="flex justify-end gap-2">
            <Button type="button" variant="ghost" size="sm" onClick={() => setOpen(false)}>
              {t('common.cancel')}
            </Button>
            <Button type="button" size="sm" disabled={!draft.from} onClick={apply}>
              {t('reports.apply')}
            </Button>
          </div>
        </div>
      </PopoverContent>
    </Popover>
  )
}
