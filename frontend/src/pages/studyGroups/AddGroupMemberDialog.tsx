import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { addGroupMember } from '@/features/studyGroups/api'
import { ApiError } from '@/api/errors'
import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Field } from '@/components/shared/Field'
import { EnrollmentPicker } from '@/components/shared/EnrollmentPicker'

interface AddGroupMemberDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  groupId: string
  courseId: string
  onAdded: () => void
}

export function AddGroupMemberDialog({
  open,
  onOpenChange,
  groupId,
  courseId,
  onAdded,
}: AddGroupMemberDialogProps) {
  const { t } = useTranslation()
  const [enrollment, setEnrollment] = useState<{ id: string; label: string } | null>(null)
  const [pending, setPending] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function handleSubmit() {
    if (!enrollment) return
    setPending(true)
    setError(null)
    try {
      await addGroupMember(groupId, enrollment.id)
      toast.success(t('studyGroups.detail.memberAdded'))
      onAdded()
      setEnrollment(null)
      onOpenChange(false)
    } catch (err) {
      setError(
        err instanceof ApiError ? err.detail || t('common.unknownError') : t('common.networkError'),
      )
    } finally {
      setPending(false)
    }
  }

  return (
    <Dialog
      open={open}
      onOpenChange={(next) => {
        if (!next) setEnrollment(null)
        onOpenChange(next)
      }}
    >
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{t('studyGroups.addMemberDialog.title')}</DialogTitle>
        </DialogHeader>
        <Field label={t('studyGroups.addMemberDialog.pickEnrollment')} error={error ?? undefined}>
          <EnrollmentPicker
            value={enrollment}
            onChange={setEnrollment}
            courseId={courseId}
            studentSearchPlaceholder={t('studyGroups.addMemberDialog.studentSearchPlaceholder')}
            emptyMessage={t('studyGroups.addMemberDialog.noEligibleEnrollment')}
          />
        </Field>
        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)} disabled={pending}>
            {t('common.cancel')}
          </Button>
          <Button onClick={handleSubmit} disabled={!enrollment} loading={pending}>
            {t('studyGroups.addMemberDialog.submit')}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
