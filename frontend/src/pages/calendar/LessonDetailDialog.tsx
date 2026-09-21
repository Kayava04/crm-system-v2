import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { rescheduleLesson, cancelLesson, completeLesson } from '@/features/scheduling/api'
import type { ResolvedScheduleRow } from '@/features/scheduling/useResolvedSchedules'
import { ApiError } from '@/api/errors'
import { enumLabel } from '@/lib/enumLabels'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { ConfirmDialog } from '@/components/shared/ConfirmDialog'
import { Field } from '@/components/shared/Field'

function statusVariant(status: string): 'success' | 'warning' | 'secondary' | 'destructive' {
  if (status === 'Scheduled' || status === 'Rescheduled') return 'success'
  if (status === 'Completed') return 'secondary'
  return 'destructive'
}

interface LessonDetailDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  resolved: ResolvedScheduleRow | null
  onChanged: () => void
}

export function LessonDetailDialog({
  open,
  onOpenChange,
  resolved,
  onChanged,
}: LessonDetailDialogProps) {
  const { t } = useTranslation()
  const [reschedOpen, setReschedOpen] = useState(false)
  const [reschedValue, setReschedValue] = useState('')
  const [cancelOpen, setCancelOpen] = useState(false)
  const [completeOpen, setCompleteOpen] = useState(false)
  const [pending, setPending] = useState(false)

  if (!resolved) return null
  const { row } = resolved
  const isActive = row.status === 'Scheduled' || row.status === 'Rescheduled'

  async function handleReschedule() {
    if (!reschedValue) return
    setPending(true)
    try {
      await rescheduleLesson(row.id, new Date(reschedValue).toISOString())
      toast.success(t('calendar.item.rescheduledToast'))
      onChanged()
      setReschedOpen(false)
      onOpenChange(false)
    } catch (err) {
      toast.error(
        err instanceof ApiError ? err.detail || t('common.unknownError') : t('common.networkError'),
      )
    } finally {
      setPending(false)
    }
  }

  async function handleCancel() {
    await cancelLesson(row.id)
    toast.success(t('calendar.item.cancelledToast'))
    onChanged()
    onOpenChange(false)
  }

  async function handleComplete() {
    await completeLesson(row.id)
    toast.success(t('calendar.item.completedToast'))
    onChanged()
    onOpenChange(false)
  }

  return (
    <>
      <Dialog open={open} onOpenChange={onOpenChange}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              {t('calendar.item.title')}
              <Badge variant={statusVariant(row.status)}>
                {enumLabel(t, 'scheduleStatus', row.status)}
              </Badge>
            </DialogTitle>
          </DialogHeader>
          <div className="flex flex-col gap-3 text-sm">
            <div className="flex justify-between">
              <span className="text-muted-foreground">{t('calendar.item.teacher')}</span>
              <span>{resolved.teacherName ?? '—'}</span>
            </div>
            {resolved.groupName ? (
              <div className="flex justify-between">
                <span className="text-muted-foreground">{t('calendar.item.group')}</span>
                <span>{resolved.groupName}</span>
              </div>
            ) : (
              <div className="flex justify-between">
                <span className="text-muted-foreground">{t('calendar.item.students')}</span>
                <span>{resolved.studentName ?? '—'}</span>
              </div>
            )}
            <div className="flex justify-between">
              <span className="text-muted-foreground">{t('calendar.item.course')}</span>
              <span>{resolved.courseName ?? '—'}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-muted-foreground">{t('calendar.item.duration')}</span>
              <span>{Number(row.durationMinutes)}</span>
            </div>
          </div>

          {isActive && (
            <DialogFooter className="flex-wrap gap-2">
              <Button variant="outline" onClick={() => setReschedOpen(true)}>
                {t('calendar.item.reschedule')}
              </Button>
              <Button variant="outline" onClick={() => setCompleteOpen(true)}>
                {t('calendar.item.complete')}
              </Button>
              <Button variant="destructive" onClick={() => setCancelOpen(true)}>
                {t('calendar.item.cancel')}
              </Button>
            </DialogFooter>
          )}
        </DialogContent>
      </Dialog>

      <Dialog open={reschedOpen} onOpenChange={setReschedOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t('calendar.item.reschedule')}</DialogTitle>
          </DialogHeader>
          <Field label={t('calendar.item.rescheduleLabel')}>
            <Input
              type="datetime-local"
              value={reschedValue}
              onChange={(e) => setReschedValue(e.target.value)}
            />
          </Field>
          <DialogFooter>
            <Button variant="outline" onClick={() => setReschedOpen(false)} disabled={pending}>
              {t('common.cancel')}
            </Button>
            <Button onClick={handleReschedule} loading={pending}>
              {t('calendar.item.rescheduleSubmit')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <ConfirmDialog
        open={cancelOpen}
        onOpenChange={setCancelOpen}
        title={t('calendar.item.confirmCancelTitle')}
        description={t('calendar.item.confirmCancelDesc')}
        destructive
        onConfirm={handleCancel}
      />
      <ConfirmDialog
        open={completeOpen}
        onOpenChange={setCompleteOpen}
        title={t('calendar.item.confirmCompleteTitle')}
        description={t('calendar.item.confirmCompleteDesc')}
        onConfirm={handleComplete}
      />
    </>
  )
}
