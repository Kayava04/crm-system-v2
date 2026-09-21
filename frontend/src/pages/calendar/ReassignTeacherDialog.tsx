import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { reassignTeacher } from '@/features/scheduling/api'
import { ApiError } from '@/api/errors'
import { toNum } from '@/lib/utils'
import { Button } from '@/components/ui/button'
import { Checkbox } from '@/components/ui/checkbox'
import { Label } from '@/components/ui/label'
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

interface ReassignTeacherDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onReassigned: () => void
}

export function ReassignTeacherDialog({
  open,
  onOpenChange,
  onReassigned,
}: ReassignTeacherDialogProps) {
  const { t } = useTranslation()
  const [fromTeacher, setFromTeacher] = useState<{ id: string; fullName: string } | null>(null)
  const [toTeacher, setToTeacher] = useState<{ id: string; fullName: string } | null>(null)
  const [updateGroups, setUpdateGroups] = useState(true)
  const [pending, setPending] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)

  function handleClose(next: boolean) {
    if (!next) {
      setFromTeacher(null)
      setToTeacher(null)
      setUpdateGroups(true)
      setFormError(null)
    }
    onOpenChange(next)
  }

  async function handleSubmit() {
    setFormError(null)
    if (!fromTeacher || !toTeacher) {
      setFormError(t('calendar.reassignDialog.fromLabel'))
      return
    }
    setPending(true)
    try {
      const result = await reassignTeacher({
        fromTeacherId: fromTeacher.id,
        toTeacherId: toTeacher.id,
        updateGroups,
      })
      const lines = [
        t('calendar.reassignDialog.reassignedCount', { count: toNum(result.reassignedCount) }),
        toNum(result.restoredCount)
          ? t('calendar.reassignDialog.restoredCount', { count: toNum(result.restoredCount) })
          : null,
        toNum(result.updatedGroupsCount)
          ? t('calendar.reassignDialog.updatedGroupsCount', {
              count: toNum(result.updatedGroupsCount),
            })
          : null,
      ].filter(Boolean)
      toast.success(t('calendar.reassignDialog.success'), { description: lines.join(' · ') })
      onReassigned()
      handleClose(false)
    } catch (err) {
      setFormError(
        err instanceof ApiError ? err.detail || t('common.unknownError') : t('common.networkError'),
      )
    } finally {
      setPending(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={handleClose}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{t('calendar.reassignTeacher')}</DialogTitle>
          <DialogDescription>{t('calendar.reassignDialog.description')}</DialogDescription>
        </DialogHeader>
        <div className="flex flex-col gap-4">
          {formError && <p className="text-sm text-destructive">{formError}</p>}
          <Field label={t('calendar.reassignDialog.fromLabel')}>
            <TeacherSearchInput value={fromTeacher} onChange={setFromTeacher} />
          </Field>
          <Field label={t('calendar.reassignDialog.toLabel')}>
            <TeacherSearchInput value={toTeacher} onChange={setToTeacher} />
          </Field>
          <div className="flex items-center gap-2">
            <Checkbox
              id="updateGroups"
              checked={updateGroups}
              onCheckedChange={(v) => setUpdateGroups(v === true)}
            />
            <Label htmlFor="updateGroups" className="font-normal">
              {t('calendar.reassignDialog.updateGroupsLabel')}
            </Label>
          </div>
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={() => handleClose(false)} disabled={pending}>
            {t('common.cancel')}
          </Button>
          <Button onClick={handleSubmit} loading={pending}>
            {t('calendar.reassignDialog.submit')}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
