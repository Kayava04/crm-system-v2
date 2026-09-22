import { forwardRef, useImperativeHandle, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { Plus, Pencil, Trash2 } from 'lucide-react'
import {
  getCalendarEvents,
  deleteCalendarEvent,
  type CalendarEvent,
} from '@/features/scheduling/calendarEvents'
import { useCan } from '@/features/auth/useCan'
import { ApiError } from '@/api/errors'
import { formatDateTime, formatDate } from '@/lib/utils'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import { ConfirmDialog } from '@/components/shared/ConfirmDialog'
import { CalendarEventDialog } from './CalendarEventDialog'

interface CalendarEventsPanelProps {
  from: string
  to: string
  /** Staff view triggers "add" from its own toolbar (see StaffCalendarView) instead of
   * from this panel's own header, since one merged "+ New event" button up there replaces
   * both this panel's create actions and the lesson-creation button that used to live there. */
  showAddButton?: boolean
}

export interface CalendarEventsPanelHandle {
  openCreate: (visibility: 'Personal' | 'Everyone') => void
}

/** A compact list of arbitrary calendar entries (not lessons) for the week
 * currently shown by the parent view: the caller's own personal reminders,
 * plus every school-wide notice. Mounted by both MyCalendarView and
 * StaffCalendarView, sharing the same from/to as their own lesson grid. */
export const CalendarEventsPanel = forwardRef<CalendarEventsPanelHandle, CalendarEventsPanelProps>(
  function CalendarEventsPanel({ from, to, showAddButton = true }, ref) {
    const { t, i18n } = useTranslation()
    const lang = i18n.language === 'en' ? 'en' : 'uk'
    const canManageSchedule = useCan('CanManageSchedule')
    const queryClient = useQueryClient()

    const [dialogOpen, setDialogOpen] = useState(false)
    const [editing, setEditing] = useState<CalendarEvent | null>(null)
    const [creatingVisibility, setCreatingVisibility] = useState<'Personal' | 'Everyone'>('Personal')
    const [deleting, setDeleting] = useState<CalendarEvent | null>(null)

    const { data, isLoading } = useQuery({
      queryKey: ['calendar', 'events', from, to],
      queryFn: () => getCalendarEvents(from, to),
    })

    function refresh() {
      void queryClient.invalidateQueries({ queryKey: ['calendar', 'events'] })
    }

    function openCreate(visibility: 'Personal' | 'Everyone') {
      setEditing(null)
      setCreatingVisibility(visibility)
      setDialogOpen(true)
    }

    useImperativeHandle(ref, () => ({ openCreate }))

    function openEdit(event: CalendarEvent) {
      setEditing(event)
      setDialogOpen(true)
    }

    async function handleDelete() {
      if (!deleting) return
      try {
        await deleteCalendarEvent(deleting.id)
        toast.success(t('calendarEvents.deleted'))
        refresh()
      } catch (err) {
        toast.error(
          err instanceof ApiError ? err.detail || t('common.unknownError') : t('common.networkError'),
        )
      }
    }

    const events = [...(data ?? [])].sort((a, b) => a.startsAt.localeCompare(b.startsAt))

    return (
      <Card>
        <CardHeader className="flex flex-row flex-wrap items-center justify-between gap-2">
          <CardTitle className="text-base font-medium">{t('calendarEvents.title')}</CardTitle>
          {showAddButton && (
            <Button size="sm" variant="outline" onClick={() => openCreate('Personal')}>
              <Plus />
              {t('calendarEvents.addPersonal')}
            </Button>
          )}
        </CardHeader>
        <CardContent className="flex flex-col gap-2">
          {isLoading && (
            <div className="flex flex-col gap-2">
              <Skeleton className="h-10 w-full" />
              <Skeleton className="h-10 w-full" />
            </div>
          )}
          {!isLoading && events.length === 0 && (
            <p className="text-sm text-muted-foreground">{t('calendarEvents.empty')}</p>
          )}
          {events.map((event) => {
            // Mirrors the server's own rule (see CalendarEventAccess): a personal event is
            // manageable by its owner, an everyone event by any schedule manager.
            const canManage = event.isMine || (event.visibility === 'Everyone' && canManageSchedule)

            return (
              <div
                key={event.id}
                className="flex flex-col gap-1 rounded-md border p-3 sm:flex-row sm:items-center sm:justify-between"
              >
                <div className="flex flex-col gap-1">
                  <div className="flex flex-wrap items-center gap-2">
                    <span className="font-medium">{event.title}</span>
                    <Badge variant={event.visibility === 'Everyone' ? 'default' : 'secondary'}>
                      {event.visibility === 'Everyone' ? t('calendarEvents.everyone') : t('calendarEvents.mine')}
                    </Badge>
                  </div>
                  <span className="text-sm text-muted-foreground">
                    {event.isAllDay
                      ? formatDate(event.startsAt, lang)
                      : `${formatDateTime(event.startsAt, lang)} – ${formatDateTime(event.endsAt, lang)}`}
                  </span>
                  {event.description && (
                    <span className="text-sm text-muted-foreground">{event.description}</span>
                  )}
                </div>
                {canManage && (
                  <div className="flex gap-2">
                    <Button size="icon" variant="ghost" aria-label={t('calendarEvents.edit')} onClick={() => openEdit(event)}>
                      <Pencil className="size-4" />
                    </Button>
                    <Button
                      size="icon"
                      variant="ghost"
                      aria-label={t('calendarEvents.delete')}
                      onClick={() => setDeleting(event)}
                    >
                      <Trash2 className="size-4" />
                    </Button>
                  </div>
                )}
              </div>
            )
          })}
        </CardContent>

        <CalendarEventDialog
          open={dialogOpen}
          onOpenChange={setDialogOpen}
          event={editing}
          defaultVisibility={creatingVisibility}
          onSaved={refresh}
        />

        <ConfirmDialog
          open={!!deleting}
          onOpenChange={(next) => !next && setDeleting(null)}
          title={t('calendarEvents.confirmDeleteTitle')}
          description={t('calendarEvents.confirmDeleteDesc')}
          destructive
          onConfirm={handleDelete}
        />
      </Card>
    )
  },
)
