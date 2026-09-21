import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { getScheduleById, updateSchedule } from '@/features/scheduling/api'
import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Field } from '@/components/shared/Field'
import { TeacherSearchInput } from '@/components/shared/TeacherSearchInput'

interface ReassignSelectedLessonsDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  lessonIds: string[]
  onReassigned: () => void
}

/** Reassigns the teacher on individually selected lessons rather than all of
 * one teacher's lessons at once. The backend has no bulk-by-id endpoint for
 * this (only bulk-by-teacher via PUT /api/schedules/reassign-teacher), so
 * each lesson is updated individually with PUT /api/schedules/{id} - the
 * same endpoint the single-lesson edit flow would use. That endpoint
 * replaces the whole record (teacher, duration, notes), so each lesson's
 * current duration/notes are read first via GET /api/schedules/{id} to
 * avoid blanking them out. Lessons that are no longer open (completed or
 * cancelled) are rejected by the backend and counted as failures. */
export function ReassignSelectedLessonsDialog({
  open,
  onOpenChange,
  lessonIds,
  onReassigned,
}: ReassignSelectedLessonsDialogProps) {
  const { t } = useTranslation()
  const [toTeacher, setToTeacher] = useState<{ id: string; fullName: string } | null>(null)
  const [pending, setPending] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)

  function handleClose(next: boolean) {
    if (!next) {
      setToTeacher(null)
      setFormError(null)
    }
    onOpenChange(next)
  }

  async function handleSubmit() {
    setFormError(null)
    if (!toTeacher) {
      setFormError(t('calendar.reassignSelectedDialog.toLabel'))
      return
    }
    setPending(true)
    let succeeded = 0
    let failed = 0
    for (const id of lessonIds) {
      try {
        const detail = await getScheduleById(id)
        await updateSchedule(id, {
          teacherId: toTeacher.id,
          durationMinutes: detail.durationMinutes,
          notes: detail.notes,
        })
        succeeded++
      } catch {
        failed++
      }
    }
    setPending(false)
    if (succeeded > 0) {
      toast.success(t('calendar.reassignSelectedDialog.success', { count: succeeded }))
    }
    if (failed > 0) {
      toast.error(t('calendar.reassignSelectedDialog.someFailed', { count: failed }))
    }
    onReassigned()
    handleClose(false)
  }

  return (
    <Dialog open={open} onOpenChange={handleClose}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{t('calendar.reassignSelectedDialog.title')}</DialogTitle>
          <DialogDescription>
            {t('calendar.reassignSelectedDialog.description', { count: lessonIds.length })}
          </DialogDescription>
        </DialogHeader>
        <div className="flex flex-col gap-4">
          {formError && <p className="text-sm text-destructive">{formError}</p>}
          <Field label={t('calendar.reassignSelectedDialog.toLabel')}>
            <TeacherSearchInput value={toTeacher} onChange={setToTeacher} />
          </Field>
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={() => handleClose(false)} disabled={pending}>
            {t('common.cancel')}
          </Button>
          <Button onClick={handleSubmit} loading={pending} disabled={lessonIds.length === 0}>
            {t('calendar.reassignSelectedDialog.submit')}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
