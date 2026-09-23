import { useMemo, useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useTranslation } from 'react-i18next'
import { useQuery } from '@tanstack/react-query'
import { toast } from 'sonner'
import { Plus, X } from 'lucide-react'
import { generateSchedule } from '@/features/scheduling/api'
import { getAllGroupsForLookup } from '@/features/studyGroups/api'
import { ApiError } from '@/api/errors'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
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
import { TeacherSearchInput } from '@/components/shared/TeacherSearchInput'

const DAYS = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'] as const

interface Slot {
  dayOfWeek: (typeof DAYS)[number]
  startTime: string
}

interface GenerateScheduleDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onGenerated: () => void
}

export function GenerateScheduleDialog({
  open,
  onOpenChange,
  onGenerated,
}: GenerateScheduleDialogProps) {
  const { t } = useTranslation()
  const [targetType, setTargetType] = useState<'individual' | 'group'>('individual')
  const [enrollment, setEnrollment] = useState<{ id: string; label: string } | null>(null)
  const [groupId, setGroupId] = useState('')
  const [teacher, setTeacher] = useState<{ id: string; fullName: string } | null>(null)
  const [slots, setSlots] = useState<Slot[]>([{ dayOfWeek: 'Monday', startTime: '10:00' }])
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
        startDate: z.string().optional(),
        lessonsCount: z.string().optional(),
        durationMinutes: z.coerce.number().int().min(1),
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
      setSlots([{ dayOfWeek: 'Monday', startTime: '10:00' }])
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
    try {
      const result = await generateSchedule({
        enrollmentId: targetType === 'individual' ? enrollment!.id : null,
        groupId: targetType === 'group' ? groupId : null,
        teacherId: teacher?.id ?? null,
        slots,
        durationMinutes: values.durationMinutes,
        startDate: values.startDate || null,
        lessonsCount: values.lessonsCount ? Number(values.lessonsCount) : null,
      })
      toast.success(t('calendar.generateDialog.success', { count: Number(result.createdCount) }))
      onGenerated()
      handleClose(false)
    } catch (err) {
      setFormError(
        err instanceof ApiError ? err.detail || t('common.unknownError') : t('common.networkError'),
      )
    }
  }

  function updateSlot(index: number, patch: Partial<Slot>) {
    setSlots((prev) => prev.map((s, i) => (i === index ? { ...s, ...patch } : s)))
  }

  return (
    <Dialog open={open} onOpenChange={handleClose}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{t('calendar.generateDialog.title')}</DialogTitle>
        </DialogHeader>
        <form className="flex flex-col gap-4" onSubmit={handleSubmit(onSubmit)} noValidate>
          {formError && <p className="text-sm text-destructive">{formError}</p>}

          <Field label={t('calendar.generateDialog.targetType')}>
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

          <Field label={`${t('calendar.createDialog.teacherLabel')} (${t('common.optional')})`}>
            <TeacherSearchInput
              value={teacher}
              onChange={setTeacher}
              placeholder={t('calendar.createDialog.teacherSearchPlaceholder')}
            />
          </Field>

          <Field label={t('calendar.generateDialog.slotsLabel')}>
            <div className="flex flex-col gap-2">
              {slots.map((slot, i) => (
                <div key={i} className="flex items-center gap-2">
                  <Select
                    value={slot.dayOfWeek}
                    onValueChange={(v) => updateSlot(i, { dayOfWeek: v as Slot['dayOfWeek'] })}
                  >
                    <SelectTrigger className="flex-1">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {DAYS.map((d) => (
                        <SelectItem key={d} value={d}>
                          {t(`enums.dayOfWeek.${d}`)}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                  <Input
                    type="time"
                    className="w-28"
                    value={slot.startTime}
                    onChange={(e) => updateSlot(i, { startTime: e.target.value })}
                  />
                  <Button
                    type="button"
                    variant="ghost"
                    size="icon"
                    onClick={() => setSlots((prev) => prev.filter((_, idx) => idx !== i))}
                    disabled={slots.length === 1}
                    aria-label={t('common.remove')}
                  >
                    <X className="size-4" />
                  </Button>
                </div>
              ))}
              <Button
                type="button"
                variant="outline"
                size="sm"
                className="self-start"
                onClick={() =>
                  setSlots((prev) => [...prev, { dayOfWeek: 'Monday', startTime: '10:00' }])
                }
              >
                <Plus />
                {t('calendar.generateDialog.addSlot')}
              </Button>
            </div>
          </Field>

          <Field
            label={t('calendar.generateDialog.durationLabel')}
            error={errors.durationMinutes?.message}
          >
            <Input type="number" min={1} {...register('durationMinutes')} />
          </Field>
          <Field label={t('calendar.generateDialog.startDateLabel')}>
            <Input type="date" {...register('startDate')} />
          </Field>
          <Field label={t('calendar.generateDialog.lessonsCountLabel')}>
            <Input type="number" min={1} {...register('lessonsCount')} />
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
              {t('calendar.generateDialog.submit')}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
