import * as React from 'react'
import { useTranslation } from 'react-i18next'
import { format, isValid, parseISO, startOfDay } from 'date-fns'
import { CalendarDays } from 'lucide-react'
import { cn } from '@/lib/utils'
import { Input } from './input'
import { Button } from './button'
import { Calendar } from './calendar'
import { Popover, PopoverAnchor, PopoverContent, PopoverTrigger } from './popover'

type DateInputProps = Omit<React.InputHTMLAttributes<HTMLInputElement>, 'type'> & {
  type?: 'date' | 'datetime-local'
}

function writeNativeValue(el: HTMLInputElement, value: string) {
  const setter = Object.getOwnPropertyDescriptor(HTMLInputElement.prototype, 'value')?.set
  setter?.call(el, value)
  el.dispatchEvent(new Event('input', { bubbles: true }))
  el.dispatchEvent(new Event('change', { bubbles: true }))
}

function parseDatePart(value: string | number | readonly string[] | undefined) {
  if (typeof value !== 'string' || !value) return null
  const d = parseISO(value.slice(0, 10))
  return isValid(d) ? d : null
}

export const DateInput = React.forwardRef<HTMLInputElement, DateInputProps>(
  ({ type = 'date', className, disabled, readOnly, min, max, ...props }, ref) => {
    const { t } = useTranslation()
    const inputRef = React.useRef<HTMLInputElement | null>(null)
    const contentRef = React.useRef<HTMLDivElement>(null)
    const [open, setOpen] = React.useState(false)
    const [value, setValue] = React.useState('')

    React.useImperativeHandle(ref, () => inputRef.current as HTMLInputElement)

    const withTime = type === 'datetime-local'
    const [datePart, timePart = ''] = value.split('T')
    const selected = parseDatePart(datePart)

    function commit(next: string) {
      if (inputRef.current) writeNativeValue(inputRef.current, next)
      setValue(next)
    }

    function selectDay(day: Date) {
      const iso = format(day, 'yyyy-MM-dd')
      if (withTime) {
        commit(`${iso}T${timePart || format(new Date(), 'HH:00')}`)
      } else {
        commit(iso)
        setOpen(false)
      }
    }

    return (
      <Popover
        open={open}
        onOpenChange={(next) => {
          if (next) setValue(inputRef.current?.value ?? '')
          setOpen(next)
        }}
      >
        <PopoverAnchor asChild>
          <div className="relative w-full">
            <Input
              ref={inputRef}
              type={type}
              disabled={disabled}
              readOnly={readOnly}
              min={min}
              max={max}
              className={cn('pr-10 [&::-webkit-calendar-picker-indicator]:hidden', className)}
              {...props}
            />
            <PopoverTrigger asChild>
              <button
                type="button"
                disabled={disabled || readOnly}
                aria-label={t('datePicker.open')}
                className="absolute top-1/2 right-1 flex size-7 -translate-y-1/2 items-center justify-center rounded-md text-muted-foreground outline-none transition-[color,background-color] hover:bg-foreground/6 hover:text-foreground focus-visible:ring-2 focus-visible:ring-ring disabled:pointer-events-none disabled:opacity-50 data-[state=open]:text-primary"
              >
                <CalendarDays className="size-4" />
              </button>
            </PopoverTrigger>
          </div>
        </PopoverAnchor>
        <PopoverContent
          ref={contentRef}
          align="end"
          onOpenAutoFocus={(e) => {
            e.preventDefault()
            contentRef.current?.querySelector<HTMLElement>('[data-cursor="true"]')?.focus()
          }}
        >
          <Calendar
            selected={selected}
            onSelect={selectDay}
            min={parseDatePart(min)}
            max={parseDatePart(max)}
          />

          {withTime && (
            <div className="mt-3 flex items-center justify-between gap-3 border-t border-foreground/8 pt-3">
              <span className="text-sm text-muted-foreground">{t('datePicker.time')}</span>
              <Input
                type="time"
                value={timePart}
                disabled={!selected}
                onChange={(e) => selected && commit(`${datePart}T${e.target.value}`)}
                className="h-8 w-28 [&::-webkit-calendar-picker-indicator]:hidden"
              />
            </div>
          )}

          <div className="mt-3 flex items-center justify-between border-t border-foreground/8 pt-3">
            <Button
              type="button"
              variant="ghost"
              size="sm"
              onClick={() => {
                commit('')
                setOpen(false)
              }}
            >
              {t('datePicker.clear')}
            </Button>
            <div className="flex gap-1">
              <Button
                type="button"
                variant="ghost"
                size="sm"
                className="text-primary"
                onClick={() => selectDay(startOfDay(new Date()))}
              >
                {t('datePicker.today')}
              </Button>
              {withTime && (
                <Button type="button" size="sm" onClick={() => setOpen(false)}>
                  {t('datePicker.done')}
                </Button>
              )}
            </div>
          </div>
        </PopoverContent>
      </Popover>
    )
  },
)
DateInput.displayName = 'DateInput'
