import { useEffect, useMemo, useRef, useState, type KeyboardEvent } from 'react'
import { useTranslation } from 'react-i18next'
import {
  addDays,
  addMonths,
  addYears,
  isAfter,
  isBefore,
  isSameDay,
  isSameMonth,
  setMonth,
  setYear,
  startOfDay,
  startOfMonth,
  startOfWeek,
} from 'date-fns'
import { ChevronDown, ChevronLeft, ChevronRight } from 'lucide-react'
import { cn } from '@/lib/utils'

type View = 'days' | 'months' | 'years'

export interface DateRange {
  from: Date | null
  to: Date | null
}

interface CalendarProps {
  selected?: Date | null
  /** Range mode: both ends are highlighted with a band between them, and while
   * only `from` is set the band previews up to the hovered day. */
  range?: DateRange
  onSelect: (date: Date) => void
  min?: Date | null
  max?: Date | null
  /** Month shown first when nothing is selected yet. */
  defaultMonth?: Date
}

const YEARS_PER_PAGE = 12

function capitalize(s: string) {
  return s.charAt(0).toUpperCase() + s.slice(1)
}

/** Month grid with month/year drill-up and full keyboard support (arrows,
 * Home/End, PageUp/PageDown, Shift+Page for years). */
export function Calendar({
  selected = null,
  range,
  onSelect,
  min,
  max,
  defaultMonth,
}: CalendarProps) {
  const { t, i18n } = useTranslation()
  const locale = i18n.language === 'en' ? 'en-US' : 'uk-UA'
  const today = startOfDay(new Date())

  const [view, setView] = useState<View>('days')
  const [cursor, setCursor] = useState<Date>(() =>
    startOfDay(selected ?? range?.to ?? range?.from ?? defaultMonth ?? today),
  )
  const [hovered, setHovered] = useState<Date | null>(null)

  const rangeEnd = range?.to ?? (range?.from ? hovered : null)
  const [lo, hi] =
    range?.from && rangeEnd
      ? isBefore(rangeEnd, range.from)
        ? [rangeEnd, range.from]
        : [range.from, rangeEnd]
      : [range?.from ?? null, range?.from ?? null]
  const gridRef = useRef<HTMLDivElement>(null)
  const refocus = useRef(false)

  const month = startOfMonth(cursor)

  const gridStart = startOfWeek(month, { weekStartsOn: 1 })
  const days = Array.from({ length: 42 }, (_, i) => addDays(gridStart, i))

  const weekdays = useMemo(() => {
    const fmt = new Intl.DateTimeFormat(locale, { weekday: 'short' })
    const monday = startOfWeek(new Date(2024, 0, 1), { weekStartsOn: 1 })
    return Array.from({ length: 7 }, (_, i) => fmt.format(addDays(monday, i)))
  }, [locale])

  const monthNames = useMemo(() => {
    const fmt = new Intl.DateTimeFormat(locale, { month: 'short' })
    return Array.from({ length: 12 }, (_, i) => capitalize(fmt.format(new Date(2024, i, 1))))
  }, [locale])

  const monthLabel = capitalize(new Intl.DateTimeFormat(locale, { month: 'long' }).format(month))
  const dayLabel = new Intl.DateTimeFormat(locale, { dateStyle: 'full' })

  const isOutOfRange = (d: Date) =>
    (!!min && isBefore(d, startOfDay(min))) || (!!max && isAfter(d, startOfDay(max)))

  // Keyboard moves land on a day that may only exist after the next render
  // (a new month), so focus follows the cursor once it has been painted.
  useEffect(() => {
    if (!refocus.current) return
    refocus.current = false
    gridRef.current?.querySelector<HTMLButtonElement>('[data-cursor="true"]')?.focus()
  })

  function moveTo(next: Date) {
    refocus.current = true
    setCursor(next)
  }

  function onGridKeyDown(e: KeyboardEvent<HTMLDivElement>) {
    const steps: Record<string, number> = {
      ArrowLeft: -1,
      ArrowRight: 1,
      ArrowUp: -7,
      ArrowDown: 7,
    }
    let next: Date
    if (e.key in steps) next = addDays(cursor, steps[e.key])
    else if (e.key === 'PageUp') next = e.shiftKey ? addYears(cursor, -1) : addMonths(cursor, -1)
    else if (e.key === 'PageDown') next = e.shiftKey ? addYears(cursor, 1) : addMonths(cursor, 1)
    else if (e.key === 'Home') next = startOfWeek(cursor, { weekStartsOn: 1 })
    else if (e.key === 'End') next = addDays(startOfWeek(cursor, { weekStartsOn: 1 }), 6)
    else return
    e.preventDefault()
    moveTo(next)
  }

  const yearPageStart = cursor.getFullYear() - (cursor.getFullYear() % YEARS_PER_PAGE)

  const navButton =
    'flex size-8 items-center justify-center rounded-lg text-muted-foreground outline-none transition-[color,background-color,transform] hover:bg-foreground/6 hover:text-foreground focus-visible:ring-2 focus-visible:ring-ring active:scale-95'
  const pickButton =
    'flex h-10 items-center justify-center rounded-lg text-sm outline-none transition-[color,background-color,transform] hover:bg-foreground/6 focus-visible:ring-2 focus-visible:ring-ring active:scale-95'

  function step(direction: 1 | -1) {
    if (view === 'days') setCursor(addMonths(cursor, direction))
    else if (view === 'months') setCursor(addYears(cursor, direction))
    else setCursor(addYears(cursor, direction * YEARS_PER_PAGE))
  }

  return (
    <div className="w-70 select-none">
      <div className="mb-2 flex items-center justify-between gap-1">
        <button
          type="button"
          className="flex h-8 items-center gap-1 rounded-lg px-2 text-sm font-semibold outline-none transition-colors hover:bg-foreground/6 focus-visible:ring-2 focus-visible:ring-ring"
          onClick={() => setView(view === 'days' ? 'years' : 'days')}
          aria-label={t('datePicker.chooseYear')}
        >
          {view === 'years'
            ? `${yearPageStart} – ${yearPageStart + YEARS_PER_PAGE - 1}`
            : view === 'months'
              ? cursor.getFullYear()
              : `${monthLabel} ${cursor.getFullYear()}`}
          <ChevronDown
            className={cn(
              'size-3.5 text-muted-foreground transition-transform',
              view !== 'days' && 'rotate-180',
            )}
          />
        </button>
        <div className="flex items-center">
          <button
            type="button"
            className={navButton}
            onClick={() => step(-1)}
            aria-label={t('datePicker.previous')}
          >
            <ChevronLeft className="size-4" />
          </button>
          <button
            type="button"
            className={navButton}
            onClick={() => step(1)}
            aria-label={t('datePicker.next')}
          >
            <ChevronRight className="size-4" />
          </button>
        </div>
      </div>

      {view === 'days' && (
        <div
          ref={gridRef}
          role="grid"
          aria-label={`${monthLabel} ${cursor.getFullYear()}`}
          onKeyDown={onGridKeyDown}
          className="animate-fade-in"
        >
          <div role="row" className="mb-1 grid grid-cols-7">
            {weekdays.map((w) => (
              <span
                key={w}
                role="columnheader"
                className="flex h-8 items-center justify-center text-xs font-medium text-muted-foreground capitalize"
              >
                {w}
              </span>
            ))}
          </div>
          {Array.from({ length: 6 }, (_, row) => (
            <div key={row} role="row" className="grid grid-cols-7 gap-y-0.5">
              {days.slice(row * 7, row * 7 + 7).map((d, col) => {
                const inRange = !!lo && !!hi && !isBefore(d, lo) && !isAfter(d, hi)
                const isRangeStart = !!lo && isSameDay(d, lo)
                const isRangeEnd = !!hi && isSameDay(d, hi)
                const isSelected = range
                  ? isRangeStart || isRangeEnd
                  : !!selected && isSameDay(d, selected)
                const isToday = isSameDay(d, today)
                const isCursor = isSameDay(d, cursor)
                const disabled = isOutOfRange(d)
                return (
                  <div
                    key={d.getTime()}
                    role="gridcell"
                    className={cn(
                      'flex justify-center',
                      inRange && lo !== hi && 'bg-primary/10',
                      (isRangeStart || col === 0) && 'rounded-l-lg',
                      (isRangeEnd || col === 6) && 'rounded-r-lg',
                    )}
                  >
                    <button
                      type="button"
                      tabIndex={isCursor ? 0 : -1}
                      data-cursor={isCursor}
                      aria-selected={range ? inRange : isSelected}
                      aria-current={isToday ? 'date' : undefined}
                      aria-label={dayLabel.format(d)}
                      disabled={disabled}
                      onClick={() => onSelect(d)}
                      onFocus={() => !isCursor && setCursor(d)}
                      onMouseEnter={range ? () => setHovered(d) : undefined}
                      onMouseLeave={range ? () => setHovered(null) : undefined}
                      className={cn(
                        'relative flex size-9 items-center justify-center rounded-lg text-sm tabular-nums outline-none',
                        'transition-[color,background-color,box-shadow,transform] hover:bg-foreground/6 focus-visible:ring-2 focus-visible:ring-ring active:scale-95',
                        !isSameMonth(d, month) && 'text-muted-foreground/50',
                        isToday &&
                          !isSelected &&
                          'font-semibold text-primary after:absolute after:bottom-1 after:size-1 after:rounded-full after:bg-current',
                        isSelected &&
                          'bg-primary font-medium text-primary-foreground shadow-sm hover:bg-primary/90',
                        disabled && 'pointer-events-none opacity-30',
                      )}
                    >
                      {d.getDate()}
                    </button>
                  </div>
                )
              })}
            </div>
          ))}
        </div>
      )}

      {view === 'months' && (
        <div className="grid animate-fade-in grid-cols-3 gap-1 py-1">
          {monthNames.map((name, i) => {
            const isActive =
              selected?.getFullYear() === cursor.getFullYear() && selected?.getMonth() === i
            return (
              <button
                key={name}
                type="button"
                className={cn(
                  pickButton,
                  isActive && 'bg-primary text-primary-foreground hover:bg-primary/90',
                )}
                onClick={() => {
                  setCursor(setMonth(cursor, i))
                  setView('days')
                }}
              >
                {name}
              </button>
            )
          })}
        </div>
      )}

      {view === 'years' && (
        <div className="grid animate-fade-in grid-cols-3 gap-1 py-1">
          {Array.from({ length: YEARS_PER_PAGE }, (_, i) => yearPageStart + i).map((year) => (
            <button
              key={year}
              type="button"
              className={cn(
                pickButton,
                'tabular-nums',
                year === today.getFullYear() && 'font-semibold text-primary',
                year === selected?.getFullYear() &&
                  'bg-primary text-primary-foreground hover:bg-primary/90',
              )}
              onClick={() => {
                setCursor(setYear(cursor, year))
                setView('months')
              }}
            >
              {year}
            </button>
          ))}
        </div>
      )}
    </div>
  )
}
