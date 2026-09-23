import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { createInvoice } from '@/features/billing/api'
import { ApiError } from '@/api/errors'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Field } from '@/components/shared/Field'
import { EnrollmentPicker } from '@/components/shared/EnrollmentPicker'

interface CreateInvoiceDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onCreated: () => void
}

const schema = z.object({
  period: z.string().min(1),
  dueDate: z.string().min(1),
  notes: z.string().optional(),
})
type FormValues = z.infer<typeof schema>

export function CreateInvoiceDialog({ open, onOpenChange, onCreated }: CreateInvoiceDialogProps) {
  const { t } = useTranslation()
  const [enrollment, setEnrollment] = useState<{ id: string; label: string } | null>(null)
  const [formError, setFormError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { dueDate: new Date().toISOString().slice(0, 10) },
  })

  function handleClose(next: boolean) {
    if (!next) {
      reset()
      setEnrollment(null)
      setFormError(null)
    }
    onOpenChange(next)
  }

  async function onSubmit(values: FormValues) {
    setFormError(null)
    if (!enrollment) {
      setFormError(t('billing.invoices.createDialog.enrollmentLabel'))
      return
    }
    try {
      await createInvoice({
        enrollmentId: enrollment.id,
        period: values.period,
        dueDate: values.dueDate,
        notes: values.notes || null,
      })
      toast.success(t('billing.invoices.createDialog.success'))
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
          <DialogTitle>{t('billing.invoices.createDialog.title')}</DialogTitle>
        </DialogHeader>
        <form className="flex flex-col gap-4" onSubmit={handleSubmit(onSubmit)} noValidate>
          {formError && <p className="text-sm text-destructive">{formError}</p>}

          <Field label={t('billing.invoices.createDialog.enrollmentLabel')}>
            <EnrollmentPicker
              value={enrollment}
              onChange={setEnrollment}
              studentSearchPlaceholder={t(
                'billing.invoices.createDialog.enrollmentSearchPlaceholder',
              )}
            />
          </Field>

          <Field
            label={t('billing.invoices.createDialog.periodLabel')}
            error={errors.period?.message}
          >
            <Input {...register('period')} placeholder="2026-09" aria-invalid={!!errors.period} />
          </Field>
          <Field
            label={t('billing.invoices.createDialog.dueDateLabel')}
            error={errors.dueDate?.message}
          >
            <Input type="date" {...register('dueDate')} aria-invalid={!!errors.dueDate} />
          </Field>
          <Field label={t('billing.invoices.createDialog.notesLabel')}>
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
              {t('billing.invoices.createDialog.submit')}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
