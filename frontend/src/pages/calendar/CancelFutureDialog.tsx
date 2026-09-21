import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useQuery } from '@tanstack/react-query'
import { toast } from 'sonner'
import { cancelFutureSchedule } from '@/features/scheduling/api'
import { getAllGroupsForLookup } from '@/features/studyGroups/api'
import { ApiError } from '@/api/errors'
import { toNum } from '@/lib/utils'
import { Button } from '@/components/ui/button'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Field } from '@/components/shared/Field'
import { EnrollmentPicker } from '@/components/shared/EnrollmentPicker'

interface CancelFutureDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onCancelled: () => void
}

export function CancelFutureDialog({ open, onOpenChange, onCancelled }: CancelFutureDialogProps) {
  const { t } = useTranslation()
  const [targetType, setTargetType] = useState<'individual' | 'group'>('individual')
  const [enrollment, setEnrollment] = useState<{ id: string; label: string } | null>(null)
  const [groupId, setGroupId] = useState('')
  const [pending, setPending] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)

  const { data: groups } = useQuery({
    queryKey: ['study-groups', 'lookup-all'],
    queryFn: getAllGroupsForLookup,
    enabled: targetType === 'group',
    staleTime: 5 * 60_000,
  })

  function handleClose(next: boolean) {
    if (!next) {
      setEnrollment(null)
      setGroupId('')
      setFormError(null)
      setTargetType('individual')
    }
    onOpenChange(next)
  }

  async function handleSubmit() {
    setFormError(null)
    if (targetType === 'individual' && !enrollment) {
      setFormError(t('calendar.createDialog.enrollmentLabel'))
      return
    }
    if (targetType === 'group' && !groupId) {
      setFormError(t('calendar.createDialog.groupLabel'))
      return
    }
    setPending(true)
    try {
      const result = await cancelFutureSchedule({
        enrollmentId: targetType === 'individual' ? enrollment!.id : null,
        groupId: targetType === 'group' ? groupId : null,
      })
      toast.success(
        t('calendar.cancelFutureDialog.success', { count: toNum(result.cancelledCount) }),
      )
      onCancelled()
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
          <DialogTitle>{t('calendar.cancelFutureDialog.title')}</DialogTitle>
        </DialogHeader>
        <div className="flex flex-col gap-4">
          {formError && <p className="text-sm text-destructive">{formError}</p>}
          <Field label={t('calendar.cancelFutureDialog.targetType')}>
            <Select
              value={targetType}
              onValueChange={(v) => setTargetType(v as 'individual' | 'group')}
            >
              <SelectTrigger>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="individual">{t('calendar.createDialog.individual')}</SelectItem>
                <SelectItem value="group">{t('calendar.createDialog.group')}</SelectItem>
              </SelectContent>
            </Select>
          </Field>
          {targetType === 'individual' ? (
            <Field label={t('calendar.createDialog.enrollmentLabel')}>
              <EnrollmentPicker
                value={enrollment}
                onChange={setEnrollment}
                studentSearchPlaceholder={t('calendar.createDialog.enrollmentSearchPlaceholder')}
              />
            </Field>
          ) : (
            <Field label={t('calendar.createDialog.groupLabel')}>
              <Select value={groupId} onValueChange={setGroupId}>
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {(groups ?? []).map((g) => (
                    <SelectItem key={g.id} value={g.id}>
                      {g.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </Field>
          )}
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={() => handleClose(false)} disabled={pending}>
            {t('common.cancel')}
          </Button>
          <Button variant="destructive" onClick={handleSubmit} loading={pending}>
            {t('calendar.cancelFutureDialog.submit')}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
