import { useTranslation } from 'react-i18next'
import type { CalendarItem } from '@/features/scheduling/api'
import { enumLabel } from '@/lib/enumLabels'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'

function statusVariant(status: string): 'success' | 'secondary' | 'destructive' {
  if (status === 'Cancelled') return 'destructive'
  if (status === 'Completed') return 'success'
  return 'secondary'
}

interface MyLessonDetailDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  item: CalendarItem | null
  lang: string
}

/** Read-only lesson detail for a student's/teacher's own calendar — the
 * self-service counterpart to the staff LessonDetailDialog. It shows only
 * what GET /api/calendar/my already returns for this item (no reschedule/
 * cancel/complete actions: those endpoints require CanManageSchedule, which
 * students and teachers don't hold, so this dialog is intentionally
 * informational only). */
export function MyLessonDetailDialog({
  open,
  onOpenChange,
  item,
  lang,
}: MyLessonDetailDialogProps) {
  const { t } = useTranslation()

  if (!item) return null

  const timeLabel = `${new Date(item.startsAt).toLocaleTimeString(lang, {
    hour: '2-digit',
    minute: '2-digit',
  })} – ${new Date(item.endsAt).toLocaleTimeString(lang, { hour: '2-digit', minute: '2-digit' })}`
  const dateLabel = new Date(item.startsAt).toLocaleDateString(lang, {
    weekday: 'long',
    day: 'numeric',
    month: 'long',
  })

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            {t('calendar.item.title')}
            <Badge variant={statusVariant(item.status)}>
              {enumLabel(t, 'scheduleStatus', item.status)}
            </Badge>
          </DialogTitle>
        </DialogHeader>
        <div className="flex flex-col gap-3 text-sm">
          <div className="flex justify-between">
            <span className="text-muted-foreground">{t('calendar.item.time')}</span>
            <span className="text-right">
              {dateLabel}
              <br />
              {timeLabel}
            </span>
          </div>
          <div className="flex justify-between">
            <span className="text-muted-foreground">{t('calendar.item.course')}</span>
            <span>{item.courseName}</span>
          </div>
          <div className="flex justify-between">
            <span className="text-muted-foreground">{t('calendar.item.format')}</span>
            <span>{item.isOnline ? t('calendar.online') : t('calendar.offline')}</span>
          </div>
          {item.groupName && (
            <div className="flex justify-between">
              <span className="text-muted-foreground">{t('calendar.item.group')}</span>
              <span>{item.groupName}</span>
            </div>
          )}
          {item.teacherName && (
            <div className="flex justify-between">
              <span className="text-muted-foreground">{t('calendar.item.teacher')}</span>
              <span>{item.teacherName}</span>
            </div>
          )}
          {item.students.length > 0 && (
            <div className="flex justify-between gap-4">
              <span className="shrink-0 text-muted-foreground">{t('calendar.item.students')}</span>
              <span className="text-right">{item.students.join(', ')}</span>
            </div>
          )}
          {item.notes && (
            <div className="flex flex-col gap-1">
              <span className="text-muted-foreground">{t('calendar.item.notes')}</span>
              <span>{item.notes}</span>
            </div>
          )}
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            {t('common.close')}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
