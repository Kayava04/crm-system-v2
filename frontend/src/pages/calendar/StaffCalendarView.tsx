import { useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { ChevronLeft, ChevronRight, Plus, CalendarPlus, Users, Ban } from 'lucide-react'
import { getSchedules } from '@/features/scheduling/api'
import type { ScheduleListItem, ScheduleStatus } from '@/features/scheduling/api'
import {
  useResolvedSchedules,
  type ResolvedScheduleRow,
} from '@/features/scheduling/useResolvedSchedules'
import { getAllGroupsForLookup } from '@/features/studyGroups/api'
import { useWeekRange } from '@/lib/useWeekRange'
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
import { TeacherSearchInput } from '@/components/shared/TeacherSearchInput'
import { CreateLessonDialog } from './CreateLessonDialog'
import { GenerateScheduleDialog } from './GenerateScheduleDialog'
import { ReassignTeacherDialog } from './ReassignTeacherDialog'
import { CancelFutureDialog } from './CancelFutureDialog'
import { LessonDetailDialog } from './LessonDetailDialog'

const STATUSES: ScheduleStatus[] = ['Scheduled', 'Rescheduled', 'Completed', 'Cancelled']

function statusVariant(status: string): 'success' | 'secondary' | 'destructive' {
  if (status === 'Scheduled' || status === 'Rescheduled') return 'success'
  if (status === 'Completed') return 'secondary'
  return 'destructive'
}

export function StaffCalendarView() {
  const { t, i18n } = useTranslation()
  const lang = i18n.language === 'en' ? 'en' : 'uk'
  const queryClient = useQueryClient()
  const canManage = useCan('CanManageSchedule')

  const { from, to, weekStart, weekEnd, goPrev, goNext, goToday } = useWeekRange()
  const [teacherFilter, setTeacherFilter] = useState<{ id: string; fullName: string } | null>(null)
  const [groupFilter, setGroupFilter] = useState('')
  const [statusFilter, setStatusFilter] = useState('')

  const [createOpen, setCreateOpen] = useState(false)
  const [generateOpen, setGenerateOpen] = useState(false)
  const [reassignOpen, setReassignOpen] = useState(false)
  const [cancelFutureOpen, setCancelFutureOpen] = useState(false)
  const [selectedRow, setSelectedRow] = useState<ResolvedScheduleRow | null>(null)

  const { data: groups } = useQuery({
    queryKey: ['study-groups', 'lookup-all'],
    queryFn: getAllGroupsForLookup,
    staleTime: 5 * 60_000,
  })

  const filters = useMemo(
    () => ({
      dateFrom: from,
      dateTo: to,
      teacherId: teacherFilter?.id || undefined,
      groupId: groupFilter || undefined,
      status: (statusFilter || undefined) as ScheduleStatus | undefined,
      page: 1,
      pageSize: 200,
    }),
    [from, to, teacherFilter, groupFilter, statusFilter],
  )

  const { data, isLoading } = useQuery({
    queryKey: ['schedules', filters],
    queryFn: () => getSchedules(filters),
  })

  const rows: ScheduleListItem[] = data?.items ?? []
  const resolved = useResolvedSchedules(rows)

  const grouped = useMemo(() => {
    const byDay = new Map<string, ResolvedScheduleRow[]>()
    for (const r of resolved) {
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
      const key = cursor.toISOString().slice(0, 10)
      days.push({ date: new Date(cursor), key, items: byDay.get(key) ?? [] })
      cursor.setDate(cursor.getDate() + 1)
    }
    return days
  }, [resolved, weekStart])

  function invalidate() {
    queryClient.invalidateQueries({ queryKey: ['schedules'] })
  }

  const hasFilters = !!(teacherFilter || groupFilter || statusFilter)
  const rangeLabel = `${weekStart.toLocaleDateString(lang, { day: 'numeric', month: 'short' })} – ${weekEnd.toLocaleDateString(lang, { day: 'numeric', month: 'short' })}`

  return (
    <div className="flex flex-col gap-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-2xl font-semibold tracking-tight">{t('calendar.title')}</h1>
        {canManage && (
          <div className="flex flex-wrap gap-2">
            <Button size="sm" onClick={() => setCreateOpen(true)}>
              <Plus />
              {t('calendar.createLesson')}
            </Button>
            <Button size="sm" variant="outline" onClick={() => setGenerateOpen(true)}>
              <CalendarPlus />
              {t('calendar.generate')}
            </Button>
            <Button size="sm" variant="outline" onClick={() => setReassignOpen(true)}>
              <Users />
              {t('calendar.reassignTeacher')}
            </Button>
            <Button size="sm" variant="outline" onClick={() => setCancelFutureOpen(true)}>
              <Ban />
              {t('calendar.cancelFuture')}
            </Button>
          </div>
        )}
      </div>

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
          <div className="min-w-48">
            <TeacherSearchInput
              value={teacherFilter}
              onChange={setTeacherFilter}
              placeholder={t('calendar.filters.teacher')}
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
                setTeacherFilter(null)
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

      {!isLoading && rows.length === 0 && (
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
                {day.items.map((r) => (
                  <Card
                    key={r.row.id}
                    className="cursor-pointer transition-colors hover:bg-muted/40"
                    onClick={() => setSelectedRow(r)}
                  >
                    <CardContent className="flex flex-wrap items-center justify-between gap-3 py-3">
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
                    </CardContent>
                  </Card>
                ))}
              </div>
            </div>
          ))}
      </div>

      <CreateLessonDialog open={createOpen} onOpenChange={setCreateOpen} onCreated={invalidate} />
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
