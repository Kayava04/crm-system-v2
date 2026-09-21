import { useMemo, useState } from 'react'
import { useForm, Controller } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useTranslation } from 'react-i18next'
import { useQuery } from '@tanstack/react-query'
import { toast } from 'sonner'
import { createEnrollment } from '@/features/enrollments/api'
import { getStudents } from '@/features/students/api'
import { getAllCoursesForLookup } from '@/features/courses/api'
import { ApiError } from '@/api/errors'
import { useDebouncedValue } from '@/lib/useDebouncedValue'
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
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Field } from '@/components/shared/Field'

interface CreateEnrollmentDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  fixedStudentId?: string
  fixedStudentLabel?: string
  fixedCourseId?: string
  fixedCourseLabel?: string
  onCreated: () => void
}

export function CreateEnrollmentDialog({
  open,
  onOpenChange,
  fixedStudentId,
  fixedStudentLabel,
  fixedCourseId,
  fixedCourseLabel,
  onCreated,
}: CreateEnrollmentDialogProps) {
  const { t } = useTranslation()
  const [formError, setFormError] = useState<string | null>(null)
  const [studentQuery, setStudentQuery] = useState('')
  const debouncedStudentQuery = useDebouncedValue(studentQuery)
  const [pickedStudent, setPickedStudent] = useState<{ id: string; fullName: string } | null>(null)

  const { data: studentResults } = useQuery({
    queryKey: ['students', 'enrollment-search', debouncedStudentQuery],
    queryFn: () => getStudents({ search: debouncedStudentQuery, page: 1, pageSize: 10 }),
    enabled: !fixedStudentId && debouncedStudentQuery.length >= 2,
  })

  const { data: courses } = useQuery({
    queryKey: ['courses', 'lookup-all'],
    queryFn: getAllCoursesForLookup,
    enabled: !fixedCourseId,
    staleTime: 5 * 60_000,
  })
  const activeCourses = (courses ?? []).filter((c) => c.status === 'Active')

  const schema = useMemo(
    () =>
      z.object({
        courseId: z.string().min(1),
        startDate: z.string().min(1),
        discountedPrice: z.string().optional(),
        comment: z.string().optional(),
        preferredSchedule: z.string().optional(),
      }),
    [],
  )
  type FormValues = z.infer<typeof schema>

  const {
    control,
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      courseId: fixedCourseId ?? '',
      startDate: new Date().toISOString().slice(0, 10),
    },
  })

  function handleClose(next: boolean) {
    if (!next) {
      reset()
      setPickedStudent(null)
      setStudentQuery('')
      setFormError(null)
    }
    onOpenChange(next)
  }

  async function onSubmit(values: FormValues) {
    setFormError(null)
    const studentId = fixedStudentId ?? pickedStudent?.id
    if (!studentId) {
      setFormError(t('enrollments.createDialog.studentLabel'))
      return
    }
    try {
      await createEnrollment({
        studentId,
        courseId: values.courseId,
        startDate: values.startDate,
        discountedPrice: values.discountedPrice ? Number(values.discountedPrice) : null,
        comment: values.comment || null,
        preferredSchedule: values.preferredSchedule || null,
      })
      toast.success(t('enrollments.createDialog.success'))
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
          <DialogTitle>{t('enrollments.createDialog.title')}</DialogTitle>
        </DialogHeader>
        <form className="flex flex-col gap-4" onSubmit={handleSubmit(onSubmit)} noValidate>
          {formError && <p className="text-sm text-destructive">{formError}</p>}

          <Field label={t('enrollments.createDialog.studentLabel')}>
            {fixedStudentId ? (
              <Input value={fixedStudentLabel} disabled />
            ) : pickedStudent ? (
              <div className="flex items-center justify-between rounded-md border border-input px-3 py-2 text-sm">
                <span>{pickedStudent.fullName}</span>
                <button
                  type="button"
                  className="text-xs text-muted-foreground hover:text-foreground"
                  onClick={() => setPickedStudent(null)}
                >
                  {t('common.cancel')}
                </button>
              </div>
            ) : (
              <div className="flex flex-col gap-1">
                <Input
                  value={studentQuery}
                  onChange={(e) => setStudentQuery(e.target.value)}
                  placeholder={t('enrollments.createDialog.studentSearchPlaceholder')}
                />
                {studentResults && studentResults.items.length > 0 && (
                  <div className="flex flex-col rounded-md border border-border">
                    {studentResults.items.map((s) => (
                      <button
                        key={s.id}
                        type="button"
                        className="px-3 py-1.5 text-left text-sm hover:bg-muted"
                        onClick={() => {
                          setPickedStudent({ id: s.id, fullName: s.fullName })
                          setStudentQuery('')
                        }}
                      >
                        {s.fullName}{' '}
                        <span className="text-xs text-muted-foreground">{s.email}</span>
                      </button>
                    ))}
                  </div>
                )}
              </div>
            )}
          </Field>

          <Field label={t('enrollments.createDialog.courseLabel')} error={errors.courseId?.message}>
            {fixedCourseId ? (
              <Input value={fixedCourseLabel} disabled />
            ) : (
              <Controller
                control={control}
                name="courseId"
                render={({ field }) => (
                  <Select value={field.value} onValueChange={field.onChange}>
                    <SelectTrigger>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {activeCourses.map((c) => (
                        <SelectItem key={c.id} value={c.id}>
                          {c.name}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                )}
              />
            )}
          </Field>

          <Field
            label={t('enrollments.createDialog.startDateLabel')}
            error={errors.startDate?.message}
          >
            <Input type="date" {...register('startDate')} aria-invalid={!!errors.startDate} />
          </Field>
          <Field label={t('enrollments.createDialog.discountedPriceLabel')}>
            <Input type="number" min={0} step="0.01" {...register('discountedPrice')} />
          </Field>
          <Field label={t('enrollments.createDialog.commentLabel')}>
            <Textarea {...register('comment')} rows={2} />
          </Field>
          <Field label={t('enrollments.createDialog.preferredScheduleLabel')}>
            <Textarea {...register('preferredSchedule')} rows={2} />
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
              {t('enrollments.createDialog.submit')}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
