import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useTranslation } from 'react-i18next'
import { createProfileAccount } from '@/features/auth/api'
import { ApiError } from '@/api/errors'
import { applyServerValidation } from '@/lib/applyServerValidation'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Field } from '@/components/shared/Field'

const schema = z.object({ email: z.string().email() })
type FormValues = z.infer<typeof schema>

interface CreateProfileAccountDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  profileType: 'Student' | 'Teacher'
  profileId: string
  defaultEmail: string
  onCreated: (result: { email: string; temporaryPassword: string }) => void
}

/** Creates a login account for a Student/Teacher via `POST /api/auth/register`
 * (`createProfileAccount`), which already existed on the backend but had no
 * frontend entry point - confirmed feasible during the backend-architecture
 * review (item 8 of the feedback list), unlike several other requested items
 * that turned out to need backend changes. Shared between StudentDetailPage
 * and TeacherDetailPage since the flow is identical for both profile types. */
export function CreateProfileAccountDialog({
  open,
  onOpenChange,
  profileType,
  profileId,
  defaultEmail,
  onCreated,
}: CreateProfileAccountDialogProps) {
  const { t } = useTranslation()
  const [formError, setFormError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    reset,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    values: { email: defaultEmail },
  })

  function handleClose(next: boolean) {
    if (!next) {
      reset()
      setFormError(null)
    }
    onOpenChange(next)
  }

  async function onSubmit(values: FormValues) {
    setFormError(null)
    try {
      const result = await createProfileAccount({
        email: values.email,
        role: profileType,
        profileType,
        profileId,
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
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{t('accountLink.dialogTitle')}</DialogTitle>
          <DialogDescription>{t('accountLink.dialogDescription')}</DialogDescription>
        </DialogHeader>
        <form className="flex flex-col gap-4" onSubmit={handleSubmit(onSubmit)} noValidate>
          {formError && <p className="text-sm text-destructive">{formError}</p>}
          <Field label={t('accountLink.emailLabel')} error={errors.email?.message}>
            <Input type="email" {...register('email')} aria-invalid={!!errors.email} />
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
              {t('accountLink.submit')}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
