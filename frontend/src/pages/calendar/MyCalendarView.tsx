import { useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useQuery } from '@tanstack/react-query'
import { ChevronLeft, ChevronRight } from 'lucide-react'
import { useHasRole } from '@/features/auth/useCan'
import { getMyCalendar } from '@/features/scheduling/api'
import type { CalendarItem, ScheduleStatus } from '@/features/scheduling/api'
import { useWeekRange, toIsoDate } from '@/lib/useWeekRange'
import { enumLabel } from '@/lib/enumLabels'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { MyLessonDetailDialog } from './MyLessonDetailDialog'
import { CalendarEventsPanel } from './CalendarEventsPanel'

const STATUSES: ScheduleStatus[] = ['Scheduled', 'Completed', 'Cancelled', 'Rescheduled']

function statusVariant(status: string): 'success' | 'secondary' | 'destructive' {
  if (status === 'Cancelled') return 'destructive'
  if (status === 'Completed') return 'success'
  return 'secondary'
}

export function MyCalendarView() {
  const { t, i18n } = useTranslation()
  const lang = i18n.language === 'en' ? 'en' : 'uk'
  const isTeacher = useHasRole('Teacher')
  const { from, to, weekStart, weekEnd, goPrev, goNext, goToday } = useWeekRange()
  const [selectedItem, setSelectedItem] = useState<CalendarItem | null>(null)
  const [search, setSearch] = useState('')
  const [statusFilter, setStatusFilter] = useState('')

  const { data, isLoading } = useQuery({
    queryKey: ['calendar', 'my', from, to],
    queryFn: () => getMyCalendar(from, to),
  })

  /** The self-service `/my` calendar endpoint has no server-side search or
   * status filter params, so filtering here is client-side over the already
   * fetched week's items - the same pattern as MyStudentsPage. */
  const filteredItems = useMemo(() => {
    const term = search.trim().toLowerCase()
    return (data?.items ?? []).filter((item) => {
      if (statusFilter && item.status !== statusFilter) return false
      if (!term) return true
      const haystack = [
        item.courseName,
        item.groupName ?? '',
        // Searching by the viewer's own name would never narrow anything down:
        // the teacher's calendar never carries a teacherName, and the
        // student's calendar never carries a students list (see
        // GetMyCalendarEndpoint), so only include the field that's actually
        // meaningful for this role.
        isTeacher ? '' : (item.teacherName ?? ''),
        ...(isTeacher ? item.students : []),
        item.notes ?? '',
      ]
        .join(' ')
        .toLowerCase()
      return haystack.includes(term)
    })
  }, [data, search, statusFilter, isTeacher])

  const grouped = useMemo(() => {
    const byDay = new Map<string, CalendarItem[]>()
    for (const item of filteredItems) {
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
      const key = toIsoDate(cursor)
      days.push({ date: new Date(cursor), key, items: byDay.get(key) ?? [] })
      cursor.setDate(cursor.getDate() + 1)
    }
    return days
  }, [filteredItems, weekStart])

  const rangeLabel = `${weekStart.toLocaleDateString(lang, { day: 'numeric', month: 'short' })} – ${weekEnd.toLocaleDateString(lang, { day: 'numeric', month: 'short' })}`

  return (
    <div className="flex flex-col gap-4">
      <h1 className="text-2xl font-semibold tracking-tight">{t('calendar.title')}</h1>

      <CalendarEventsPanel from={from} to={to} />

      <div className="flex flex-col gap-3">
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

      <div className="flex flex-wrap items-end gap-3">
        <div className="min-w-56 flex-1">
          <Input
            placeholder={t(
              isTeacher
                ? 'calendar.filters.searchPlaceholderTeacher'
                : 'calendar.filters.searchPlaceholderStudent',
            )}
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
        </div>
        <Select value={statusFilter} onValueChange={(v) => setStatusFilter(v === 'any' ? '' : v)}>
          <SelectTrigger className="w-40">
            <SelectValue placeholder={t('calendar.filters.status')} />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="any">{t('calendar.filters.any')}</SelectItem>
            {STATUSES.map((s) => (
              <SelectItem key={s} value={s}>
                {enumLabel(t, 'scheduleStatus', s)}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        {(search || statusFilter) && (
          <Button
            variant="ghost"
            onClick={() => {
              setSearch('')
              setStatusFilter('')
            }}
          >
            {t('calendar.filters.clear')}
          </Button>
        )}
      </div>

      {isLoading && (
        <div className="flex flex-col gap-3">
          <Skeleton className="h-20 w-full" />
          <Skeleton className="h-20 w-full" />
        </div>
      )}

      {!isLoading && filteredItems.length === 0 && (
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
                  <Card
                    key={item.id}
                    className="cursor-pointer transition-colors hover:bg-muted/40"
                    onClick={() => setSelectedItem(item)}
                  >
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

      <MyLessonDetailDialog
        open={!!selectedItem}
        onOpenChange={(open) => !open && setSelectedItem(null)}
        item={selectedItem}
        lang={lang}
      />
    </div>
  )
}
