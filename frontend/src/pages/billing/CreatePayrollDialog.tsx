import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { createPayroll } from '@/features/billing/api'
import { ApiError } from '@/api/errors'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Field } from '@/components/shared/Field'
import { TeacherSearchInput } from '@/components/shared/TeacherSearchInput'

interface CreatePayrollDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onCreated: () => void
}

const schema = z.object({
  period: z.string().min(1),
})
type FormValues = z.infer<typeof schema>

export function CreatePayrollDialog({ open, onOpenChange, onCreated }: CreatePayrollDialogProps) {
  const { t } = useTranslation()
  const [teacher, setTeacher] = useState<{ id: string; fullName: string } | null>(null)
  const [formError, setFormError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({ resolver: zodResolver(schema) })

  function handleClose(next: boolean) {
    if (!next) {
      reset()
      setTeacher(null)
      setFormError(null)
    }
    onOpenChange(next)
  }

  async function onSubmit(values: FormValues) {
    setFormError(null)
    if (!teacher) {
      setFormError(t('billing.payroll.createDialog.teacherLabel'))
      return
    }
    try {
      await createPayroll({ teacherId: teacher.id, period: values.period })
      toast.success(t('billing.payroll.createDialog.success'))
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
          <DialogTitle>{t('billing.payroll.createDialog.title')}</DialogTitle>
        </DialogHeader>
        <form className="flex flex-col gap-4" onSubmit={handleSubmit(onSubmit)} noValidate>
          {formError && <p className="text-sm text-destructive">{formError}</p>}

          <Field label={t('billing.payroll.createDialog.teacherLabel')}>
            <TeacherSearchInput value={teacher} onChange={setTeacher} />
          </Field>
          <Field
            label={t('billing.payroll.createDialog.periodLabel')}
            error={errors.period?.message}
          >
            <Input {...register('period')} placeholder="2026-09" aria-invalid={!!errors.period} />
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
              {t('billing.payroll.createDialog.submit')}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
