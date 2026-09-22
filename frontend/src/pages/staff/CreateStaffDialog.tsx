import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useTranslation } from 'react-i18next'
import { useQuery } from '@tanstack/react-query'
import { registerStaff, getPermissionOptions } from '@/features/staff/api'
import { ApiError } from '@/api/errors'
import { applyServerValidation } from '@/lib/applyServerValidation'
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
import { PermissionCheckboxGrid } from './PermissionCheckboxGrid'

const schema = z.object({
  email: z.string().email(),
  firstName: z.string().optional(),
  lastName: z.string().optional(),
  phoneNumber: z.string().optional(),
})
type FormValues = z.infer<typeof schema>

interface CreateStaffDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onCreated: (result: { email: string; temporaryPassword: string }) => void
}

export function CreateStaffDialog({ open, onOpenChange, onCreated }: CreateStaffDialogProps) {
  const { t } = useTranslation()
  const [formError, setFormError] = useState<string | null>(null)
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set())

  const { data: options } = useQuery({
    queryKey: ['staff', 'permission-options'],
    queryFn: getPermissionOptions,
    enabled: open,
    staleTime: 5 * 60_000,
  })

  const {
    register,
    handleSubmit,
    reset,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({ resolver: zodResolver(schema) })

  function handleClose(next: boolean) {
    if (!next) {
      reset()
      setSelectedIds(new Set())
      setFormError(null)
    }
    onOpenChange(next)
  }

  async function onSubmit(values: FormValues) {
    setFormError(null)
    try {
      const result = await registerStaff({
        email: values.email,
        firstName: values.firstName || null,
        lastName: values.lastName || null,
        phoneNumber: values.phoneNumber || null,
        permissionIds: Array.from(selectedIds),
      })
      onCreated({ email: result.email, temporaryPassword: result.temporaryPassword })
      handleClose(false)
    } catch (err) {
      if (err instanceof ApiError) {
        if (err.isValidation) {
          applyServerValidation(setError, err)
          return
        }
        setFormError(err.detail || t('common.unknownError'))
        return
      }
      setFormError(t('common.networkError'))
    }
  }

  return (
    <Dialog open={open} onOpenChange={handleClose}>
      <DialogContent className="max-h-[85vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>{t('staff.createDialog.title')}</DialogTitle>
        </DialogHeader>
        <form className="flex flex-col gap-4" onSubmit={handleSubmit(onSubmit)} noValidate>
          {formError && <p className="text-sm text-destructive">{formError}</p>}

          <Field label={t('staff.createDialog.emailLabel')} error={errors.email?.message}>
            <Input type="email" {...register('email')} aria-invalid={!!errors.email} />
          </Field>
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <Field label={t('staff.createDialog.firstNameLabel')}>
              <Input {...register('firstName')} />
            </Field>
            <Field label={t('staff.createDialog.lastNameLabel')}>
              <Input {...register('lastName')} />
            </Field>
          </div>
          <Field label={t('staff.createDialog.phoneLabel')}>
            <Input {...register('phoneNumber')} />
          </Field>

          <Field label={t('staff.createDialog.permissionsLabel')}>
            <PermissionCheckboxGrid
              options={options ?? []}
              selectedIds={selectedIds}
              onChange={setSelectedIds}
            />
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
              {t('staff.createDialog.submit')}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
