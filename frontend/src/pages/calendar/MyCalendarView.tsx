import { useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { useQuery } from '@tanstack/react-query'
import { ChevronLeft, ChevronRight } from 'lucide-react'
import { getMyCalendar } from '@/features/scheduling/api'
import type { CalendarItem } from '@/features/scheduling/api'
import { useWeekRange } from '@/lib/useWeekRange'
import { enumLabel } from '@/lib/enumLabels'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'

function statusVariant(status: string): 'success' | 'secondary' | 'destructive' {
  if (status === 'Scheduled' || status === 'Rescheduled') return 'success'
  if (status === 'Completed') return 'secondary'
  return 'destructive'
}

export function MyCalendarView() {
  const { t, i18n } = useTranslation()
  const lang = i18n.language === 'en' ? 'en' : 'uk'
  const { from, to, weekStart, weekEnd, goPrev, goNext, goToday } = useWeekRange()

  const { data, isLoading } = useQuery({
    queryKey: ['calendar', 'my', from, to],
    queryFn: () => getMyCalendar(from, to),
  })

  const grouped = useMemo(() => {
    const byDay = new Map<string, CalendarItem[]>()
    for (const item of data?.items ?? []) {
      const key = item.startsAt.slice(0, 10)
      const list = byDay.get(key) ?? []
      list.push(item)
      byDay.set(key, list)
    }
    for (const list of byDay.values()) {
      list.sort((a, b) => a.startsAt.localeCompare(b.startsAt))
    }
    const days: { date: Date; key: string; items: CalendarItem[] }[] = []
    const cursor = new Date(weekStart)
    for (let i = 0; i < 7; i++) {
      const key = cursor.toISOString().slice(0, 10)
      days.push({ date: new Date(cursor), key, items: byDay.get(key) ?? [] })
      cursor.setDate(cursor.getDate() + 1)
    }
    return days
  }, [data, weekStart])

  const rangeLabel = `${weekStart.toLocaleDateString(lang, { day: 'numeric', month: 'short' })} – ${weekEnd.toLocaleDateString(lang, { day: 'numeric', month: 'short' })}`

  return (
    <div className="flex flex-col gap-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-2xl font-semibold tracking-tight">{t('calendar.title')}</h1>
        <div className="flex flex-wrap items-center gap-2">
          <Button variant="outline" size="sm" onClick={goPrev}>
            <ChevronLeft />
            {t('calendar.prev')}
          </Button>
          <Button variant="outline" size="sm" onClick={goToday}>
            {t('calendar.today')}
          </Button>
          <Button variant="outline" size="sm" onClick={goNext}>
            {t('calendar.next')}
            <ChevronRight />
          </Button>
          <span className="whitespace-nowrap text-sm text-muted-foreground">{rangeLabel}</span>
        </div>
      </div>

      {isLoading && (
        <div className="flex flex-col gap-3">
          <Skeleton className="h-20 w-full" />
          <Skeleton className="h-20 w-full" />
        </div>
      )}

      {!isLoading && (data?.items.length ?? 0) === 0 && (
        <p className="text-sm text-muted-foreground">{t('calendar.emptyRange')}</p>
      )}

      <div className="flex flex-col gap-4">
        {grouped
          .filter((day) => day.items.length > 0)
          .map((day) => (
            <div key={day.key} className="flex flex-col gap-2">
              <h2 className="text-sm font-medium text-muted-foreground">
                {day.date.toLocaleDateString(lang, {
                  weekday: 'long',
                  day: 'numeric',
                  month: 'long',
                })}
              </h2>
              <div className="flex flex-col gap-2">
                {day.items.map((item) => (
                  <Card key={item.id}>
                    <CardContent className="flex flex-wrap items-center justify-between gap-3 py-3">
                      <div className="flex flex-col gap-1">
                        <div className="flex items-center gap-2">
                          <span className="text-sm font-medium">
                            {new Date(item.startsAt).toLocaleTimeString(lang, {
                              hour: '2-digit',
                              minute: '2-digit',
                            })}
                            {' – '}
                            {new Date(item.endsAt).toLocaleTimeString(lang, {
                              hour: '2-digit',
                              minute: '2-digit',
                            })}
                          </span>
                          <Badge variant={statusVariant(item.status)}>
                            {enumLabel(t, 'scheduleStatus', item.status)}
                          </Badge>
                          <Badge variant="outline">
                            {item.isOnline ? t('calendar.online') : t('calendar.offline')}
                          </Badge>
                        </div>
                        <span className="text-sm">{item.courseName}</span>
                        <span className="text-xs text-muted-foreground">
                          {item.isGroup
                            ? `${item.groupName ?? ''}${item.students.length ? ` — ${item.students.join(', ')}` : ''}`
                            : item.students.join(', ') || item.teacherName || '—'}
                        </span>
                        {item.teacherName && (
                          <span className="text-xs text-muted-foreground">
                            {t('calendar.item.teacher')}: {item.teacherName}
                          </span>
                        )}
                        {item.notes && (
                          <span className="text-xs text-muted-foreground">{item.notes}</span>
                        )}
                      </div>
                    </CardContent>
                  </Card>
                ))}
              </div>
            </div>
          ))}
      </div>
    </div>
  )
}
