import { useMemo, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import {
  ChevronLeft,
  ChevronRight,
  Plus,
  CalendarPlus,
  Users,
  Ban,
  ListChecks,
  EyeOff,
} from 'lucide-react'
import { getSchedules } from '@/features/scheduling/api'
import type { ScheduleListItem, ScheduleStatus } from '@/features/scheduling/api'
import {
  useResolvedSchedules,
  type ResolvedScheduleRow,
} from '@/features/scheduling/useResolvedSchedules'
import { getAllGroupsForLookup } from '@/features/studyGroups/api'
import { useWeekRange, toIsoDate } from '@/lib/useWeekRange'
import { enumLabel } from '@/lib/enumLabels'
import { useCan } from '@/features/auth/useCan'
import { Button } from '@/components/ui/button'
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
import { Input } from '@/components/ui/input'
import { Checkbox } from '@/components/ui/checkbox'
import { GenerateScheduleDialog } from './GenerateScheduleDialog'
import { ReassignTeacherDialog } from './ReassignTeacherDialog'
import { ReassignSelectedLessonsDialog } from './ReassignSelectedLessonsDialog'
import { CancelFutureDialog } from './CancelFutureDialog'
import { LessonDetailDialog } from './LessonDetailDialog'
import { CalendarEventsPanel, type CalendarEventsPanelHandle } from './CalendarEventsPanel'

const STATUSES: ScheduleStatus[] = ['Scheduled', 'Rescheduled', 'Completed', 'Cancelled']

function addOneDay(isoDate: string): string {
  const d = new Date(`${isoDate}T00:00:00`)
  d.setDate(d.getDate() + 1)
  const year = d.getFullYear()
  const month = String(d.getMonth() + 1).padStart(2, '0')
  const day = String(d.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}

function statusVariant(status: string): 'success' | 'secondary' | 'destructive' {
  if (status === 'Cancelled') return 'destructive'
  if (status === 'Completed') return 'success'
  return 'secondary'
}

export function StaffCalendarView() {
  const { t, i18n } = useTranslation()
  const lang = i18n.language === 'en' ? 'en' : 'uk'
  const queryClient = useQueryClient()
  const canManage = useCan('CanManageSchedule')

  const { from, to, weekStart, weekEnd, goPrev, goNext, goToday } = useWeekRange()
  const [search, setSearch] = useState('')
  const [groupFilter, setGroupFilter] = useState('')
  const [statusFilter, setStatusFilter] = useState('')

  const eventsPanelRef = useRef<CalendarEventsPanelHandle>(null)
  const [generateOpen, setGenerateOpen] = useState(false)
  const [reassignOpen, setReassignOpen] = useState(false)
  const [cancelFutureOpen, setCancelFutureOpen] = useState(false)
  const [selectedRow, setSelectedRow] = useState<ResolvedScheduleRow | null>(null)
  const [selectMode, setSelectMode] = useState(false)
  const [selectedLessonIds, setSelectedLessonIds] = useState<Set<string>>(new Set())
  const [reassignSelectedOpen, setReassignSelectedOpen] = useState(false)
  // View-only: hides rows in this browser tab, nothing is changed on the server.
  // Lessons are never deleted from the system (see calendar.item.cancel for that),
  // so this is purely a "declutter what I'm looking at" aid.
  const [hiddenLessonIds, setHiddenLessonIds] = useState<Set<string>>(new Set())

  const { data: groups } = useQuery({
    queryKey: ['study-groups', 'lookup-all'],
    queryFn: getAllGroupsForLookup,
    staleTime: 5 * 60_000,
  })

  const filters = useMemo(
    () => ({
      dateFrom: from,
      // GET /api/schedules filters with `ScheduledDate <= dateTo` (no day
      // rollover), so a bare end-of-week date would cut off that whole last
      // day's lessons. Ask for one day past `to` to include it fully.
      dateTo: addOneDay(to),
      groupId: groupFilter || undefined,
      status: (statusFilter || undefined) as ScheduleStatus | undefined,
      page: 1,
      pageSize: 200,
    }),
    [from, to, groupFilter, statusFilter],
  )

  const { data, isLoading } = useQuery({
    queryKey: ['schedules', filters],
    queryFn: () => getSchedules(filters),
  })

  const rows: ScheduleListItem[] = data?.items ?? []
  const resolved = useResolvedSchedules(rows)

  const filteredResolved = useMemo(() => {
    const term = search.trim().toLowerCase()
    return resolved.filter((r) => {
      if (hiddenLessonIds.has(r.row.id)) return false
      if (!term) return true
      const haystack = [r.courseName, r.teacherName, r.groupName, r.studentName]
        .filter(Boolean)
        .join(' ')
        .toLowerCase()
      return haystack.includes(term)
    })
  }, [resolved, search, hiddenLessonIds])

  const grouped = useMemo(() => {
    const byDay = new Map<string, ResolvedScheduleRow[]>()
    for (const r of filteredResolved) {
      const key = r.row.scheduledDate.slice(0, 10)
      const list = byDay.get(key) ?? []
      list.push(r)
      byDay.set(key, list)
    }
    for (const list of byDay.values()) {
      list.sort((a, b) => a.row.scheduledDate.localeCompare(b.row.scheduledDate))
    }
    const days: { date: Date; key: string; items: ResolvedScheduleRow[] }[] = []
    const cursor = new Date(weekStart)
    for (let i = 0; i < 7; i++) {
      const key = toIsoDate(cursor)
      days.push({ date: new Date(cursor), key, items: byDay.get(key) ?? [] })
      cursor.setDate(cursor.getDate() + 1)
    }
    return days
  }, [filteredResolved, weekStart])

  function invalidate() {
    queryClient.invalidateQueries({ queryKey: ['schedules'] })
  }

  function toggleSelectMode() {
    setSelectMode((prev) => !prev)
    setSelectedLessonIds(new Set())
  }

  function toggleLessonSelected(id: string, checked: boolean) {
    setSelectedLessonIds((prev) => {
      const next = new Set(prev)
      if (checked) next.add(id)
      else next.delete(id)
      return next
    })
  }

  function hideSelectedLessons() {
    setHiddenLessonIds((prev) => new Set([...prev, ...selectedLessonIds]))
    setSelectedLessonIds(new Set())
  }

  const hasFilters = !!(search || groupFilter || statusFilter)
  const rangeLabel = `${weekStart.toLocaleDateString(lang, { day: 'numeric', month: 'short' })} – ${weekEnd.toLocaleDateString(lang, { day: 'numeric', month: 'short' })}`

  return (
    <div className="flex flex-col gap-4">
      <div className="flex flex-col gap-3">
        <h1 className="text-2xl font-semibold tracking-tight">{t('calendar.title')}</h1>
        <div className="flex flex-wrap justify-end gap-2">
          {/* Creating a personal reminder needs no special permission; only "everyone" does,
           * which the dialog itself offers to a CanManageSchedule holder - so this one button
           * replaces both the old two-button pair here and the lesson-creation button below. */}
          <Button size="sm" onClick={() => eventsPanelRef.current?.openCreate('Personal')}>
            <Plus />
            {t('calendar.newEvent')}
          </Button>
          {canManage && (
            <>
              <Button size="sm" variant="outline" onClick={() => setGenerateOpen(true)}>
                <CalendarPlus />
                {t('calendar.generate')}
              </Button>
              <Button size="sm" variant="outline" onClick={() => setReassignOpen(true)}>
                <Users />
                {t('calendar.reassignTeacher')}
              </Button>
              <Button
                size="sm"
                variant={selectMode ? 'default' : 'outline'}
                onClick={toggleSelectMode}
              >
                <ListChecks />
                {selectMode ? t('calendar.exitSelectLessons') : t('calendar.selectLessons')}
              </Button>
              <Button size="sm" variant="outline" onClick={() => setCancelFutureOpen(true)}>
                <Ban />
                {t('calendar.cancelFuture')}
              </Button>
            </>
          )}
        </div>
      </div>

      <CalendarEventsPanel ref={eventsPanelRef} from={from} to={to} showAddButton={false} />

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

        <div className="flex flex-wrap items-end gap-3">
          <div className="min-w-56 flex-1">
            <Input
              placeholder={t('calendar.filters.searchPlaceholder')}
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
          </div>
          <Select value={groupFilter} onValueChange={(v) => setGroupFilter(v === 'any' ? '' : v)}>
            <SelectTrigger className="w-40">
              <SelectValue placeholder={t('calendar.filters.group')} />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="any">{t('calendar.filters.any')}</SelectItem>
              {groups?.map((g) => (
                <SelectItem key={g.id} value={g.id}>
                  {g.name}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
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
          {hasFilters && (
            <Button
              variant="ghost"
              size="sm"
              onClick={() => {
                setSearch('')
                setGroupFilter('')
                setStatusFilter('')
              }}
            >
              {t('calendar.filters.clear')}
            </Button>
          )}
        </div>
      </div>

      {isLoading && (
        <div className="flex flex-col gap-3">
          <Skeleton className="h-20 w-full" />
          <Skeleton className="h-20 w-full" />
        </div>
      )}

      {!isLoading && filteredResolved.length === 0 && (
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
                {day.items.map((r) => {
                  const isOpenLesson =
                    r.row.status === 'Scheduled' || r.row.status === 'Rescheduled'
                  return (
                    <Card
                      key={r.row.id}
                      className="cursor-pointer transition-colors hover:bg-muted/40"
                      onClick={() => (selectMode ? undefined : setSelectedRow(r))}
                    >
                      <CardContent className="flex flex-wrap items-center justify-between gap-3 py-3">
                        <div className="flex items-center gap-3">
                          {selectMode && (
                            <Checkbox
                              checked={selectedLessonIds.has(r.row.id)}
                              disabled={!isOpenLesson}
                              onClick={(e) => e.stopPropagation()}
                              onCheckedChange={(checked) =>
                                toggleLessonSelected(r.row.id, checked === true)
                              }
                            />
                          )}
                          <div className="flex flex-col gap-1">
                            <div className="flex items-center gap-2">
                              <span className="text-sm font-medium">
                                {new Date(r.row.scheduledDate).toLocaleTimeString(lang, {
                                  hour: '2-digit',
                                  minute: '2-digit',
                                })}
                              </span>
                              <Badge variant={statusVariant(r.row.status)}>
                                {enumLabel(t, 'scheduleStatus', r.row.status)}
                              </Badge>
                            </div>
                            <span className="text-sm">{r.courseName ?? '—'}</span>
                            <span className="text-xs text-muted-foreground">
                              {r.groupName ? r.groupName : (r.studentName ?? '—')}
                              {' · '}
                              {r.teacherName ?? '—'}
                            </span>
                          </div>
                        </div>
                      </CardContent>
                    </Card>
                  )
                })}
              </div>
            </div>
          ))}
      </div>

      {selectMode && selectedLessonIds.size > 0 && (
        <div className="sticky bottom-4 z-10 flex flex-wrap items-center justify-between gap-3 rounded-lg border border-border bg-card p-3 shadow-lg">
          <span className="text-sm font-medium">
            {t('calendar.selectedCount', { count: selectedLessonIds.size })}
          </span>
          <div className="flex gap-2">
            <Button size="sm" variant="ghost" onClick={() => setSelectedLessonIds(new Set())}>
              {t('calendar.clearSelection')}
            </Button>
            <Button size="sm" onClick={() => setReassignSelectedOpen(true)}>
              <Users />
              {t('calendar.reassignSelectedTeacher')}
            </Button>
            <Button size="sm" variant="outline" onClick={hideSelectedLessons}>
              <EyeOff />
              {t('calendar.hideSelected')}
            </Button>
          </div>
        </div>
      )}

      <GenerateScheduleDialog
        open={generateOpen}
        onOpenChange={setGenerateOpen}
        onGenerated={invalidate}
      />
      <ReassignTeacherDialog
        open={reassignOpen}
        onOpenChange={setReassignOpen}
        onReassigned={invalidate}
      />
      <ReassignSelectedLessonsDialog
        open={reassignSelectedOpen}
        onOpenChange={setReassignSelectedOpen}
        lessonIds={Array.from(selectedLessonIds)}
        onReassigned={() => {
          invalidate()
          setSelectedLessonIds(new Set())
          setSelectMode(false)
        }}
      />
      <CancelFutureDialog
        open={cancelFutureOpen}
        onOpenChange={setCancelFutureOpen}
        onCancelled={invalidate}
      />
      <LessonDetailDialog
        open={!!selectedRow}
        onOpenChange={(open) => !open && setSelectedRow(null)}
        resolved={selectedRow}
        onChanged={invalidate}
      />
    </div>
  )
}
