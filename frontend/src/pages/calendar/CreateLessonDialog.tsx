import { useMemo, useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useTranslation } from 'react-i18next'
import { useQuery } from '@tanstack/react-query'
import { toast } from 'sonner'
import { createSchedule } from '@/features/scheduling/api'
import { getAllGroupsForLookup } from '@/features/studyGroups/api'
import { ApiError } from '@/api/errors'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'
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
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Field } from '@/components/shared/Field'
import { EnrollmentPicker } from '@/components/shared/EnrollmentPicker'
import { TeacherSearchInput } from '@/components/shared/TeacherSearchInput'

interface CreateLessonDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onCreated: () => void
}

export function CreateLessonDialog({ open, onOpenChange, onCreated }: CreateLessonDialogProps) {
  const { t } = useTranslation()
  const [targetType, setTargetType] = useState<'individual' | 'group'>('individual')
  const [enrollment, setEnrollment] = useState<{ id: string; label: string } | null>(null)
  const [groupId, setGroupId] = useState('')
  const [teacher, setTeacher] = useState<{ id: string; fullName: string } | null>(null)
  const [formError, setFormError] = useState<string | null>(null)

  const { data: groups } = useQuery({
    queryKey: ['study-groups', 'lookup-all'],
    queryFn: getAllGroupsForLookup,
    enabled: targetType === 'group',
    staleTime: 5 * 60_000,
  })

  const schema = useMemo(
    () =>
      z.object({
        scheduledDate: z.string().min(1),
        durationMinutes: z.coerce.number().int().min(1),
        notes: z.string().optional(),
      }),
    [],
  )
  type FormValues = z.infer<typeof schema>

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { durationMinutes: 60 },
  })

  function handleClose(next: boolean) {
    if (!next) {
      reset()
      setEnrollment(null)
      setGroupId('')
      setTeacher(null)
      setFormError(null)
      setTargetType('individual')
    }
    onOpenChange(next)
  }

  async function onSubmit(values: FormValues) {
    setFormError(null)
    if (targetType === 'individual' && !enrollment) {
      setFormError(t('calendar.createDialog.enrollmentLabel'))
      return
    }
    if (targetType === 'group' && !groupId) {
      setFormError(t('calendar.createDialog.groupLabel'))
      return
    }
    if (!teacher) {
      setFormError(t('calendar.createDialog.teacherLabel'))
      return
    }
    try {
      await createSchedule({
        enrollmentId: targetType === 'individual' ? enrollment!.id : null,
        groupId: targetType === 'group' ? groupId : null,
        teacherId: teacher.id,
        scheduledDate: new Date(values.scheduledDate).toISOString(),
        durationMinutes: values.durationMinutes,
        notes: values.notes || null,
      })
      toast.success(t('calendar.createDialog.success'))
      onCreated()
      handleClose(false)
    } catch (err) {
      setFormError(
        err instanceof ApiError ? err.detail || t('common.unknownError') : t('common.networkError'),
      )
    }
  }

  return (
    <Dialog open={open} onOpenChange={handleClose}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{t('calendar.createDialog.title')}</DialogTitle>
          <DialogDescription>{t('calendar.createDialog.description')}</DialogDescription>
        </DialogHeader>
        <form className="flex flex-col gap-4" onSubmit={handleSubmit(onSubmit)} noValidate>
          {formError && <p className="text-sm text-destructive">{formError}</p>}

          <Field label={t('calendar.createDialog.targetType')}>
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

          <Field label={t('calendar.createDialog.teacherLabel')}>
            <TeacherSearchInput
              value={teacher}
              onChange={setTeacher}
              placeholder={t('calendar.createDialog.teacherSearchPlaceholder')}
            />
          </Field>

          <Field label={t('calendar.createDialog.dateLabel')} error={errors.scheduledDate?.message}>
            <Input
              type="datetime-local"
              {...register('scheduledDate')}
              aria-invalid={!!errors.scheduledDate}
            />
          </Field>
          <Field
            label={t('calendar.createDialog.durationLabel')}
            error={errors.durationMinutes?.message}
          >
            <Input type="number" min={1} {...register('durationMinutes')} />
          </Field>
          <Field label={t('calendar.createDialog.notesLabel')}>
            <Textarea {...register('notes')} rows={2} />
          </Field>

          <DialogFooter>
            <Button
              type="button"
              variant="outline"
              onClick={() => handleClose(false)}
              disabled={isSubmitting}
            >
              {t('common.cancel')}
            </Button>
            <Button type="submit" loading={isSubmitting}>
              {t('calendar.createDialog.submit')}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
