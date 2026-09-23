import { useMemo, useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { useAuth } from './useAuth'
import { ApiError } from '@/api/errors'
import { applyServerValidation } from '@/lib/applyServerValidation'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Alert, AlertDescription } from '@/components/ui/alert'

export function ChangePasswordForm({ onSuccess }: { onSuccess?: () => void }) {
  const { t } = useTranslation()
  const { changePassword, logout } = useAuth()
  const [formError, setFormError] = useState<string | null>(null)

  const schema = useMemo(
    () =>
      z
        .object({
          oldPassword: z.string().min(1, t('auth.oldPasswordLabel')),
          newPassword: z.string().min(8, t('auth.passwordMinLength')),
          confirmNewPassword: z.string().min(1, t('auth.confirmNewPasswordLabel')),
        })
        .refine((v) => v.newPassword === v.confirmNewPassword, {
          message: t('auth.passwordsDontMatch'),
          path: ['confirmNewPassword'],
        }),
    [t],
  )
  type FormValues = z.infer<typeof schema>

  const {
    register,
    handleSubmit,
    reset,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({ resolver: zodResolver(schema) })

  async function onSubmit(values: FormValues) {
    setFormError(null)
    try {
      await changePassword(values)
      toast.success(t('auth.passwordChanged'))
      reset()
      onSuccess?.()
    } catch (err) {
      if (err instanceof ApiError) {
        if (err.isValidation) {
          applyServerValidation(setError, err)
          return
        }
        if (err.isUnauthorized) {
          setFormError(t('auth.sessionExpired'))
          logout()
          return
        }
        setFormError(err.detail || t('common.unknownError'))
        return
      }
      setFormError(t('common.networkError'))
    }
  }

  return (
    <form className="flex flex-col gap-4" onSubmit={handleSubmit(onSubmit)} noValidate>
      {formError && (
        <Alert variant="destructive">
          <AlertDescription>{formError}</AlertDescription>
        </Alert>
      )}
      <div className="flex flex-col gap-1.5">
        <Label htmlFor="oldPassword">{t('auth.oldPasswordLabel')}</Label>
        <Input
          id="oldPassword"
          type="password"
          autoComplete="current-password"
          aria-invalid={!!errors.oldPassword}
          {...register('oldPassword')}
        />
        {errors.oldPassword && (
          <p className="text-xs text-destructive">{errors.oldPassword.message}</p>
        )}
      </div>
      <div className="flex flex-col gap-1.5">
        <Label htmlFor="newPassword">{t('auth.newPasswordLabel')}</Label>
        <Input
          id="newPassword"
          type="password"
          autoComplete="new-password"
          aria-invalid={!!errors.newPassword}
          {...register('newPassword')}
        />
        {errors.newPassword && (
          <p className="text-xs text-destructive">{errors.newPassword.message}</p>
        )}
      </div>
      <div className="flex flex-col gap-1.5">
        <Label htmlFor="confirmNewPassword">{t('auth.confirmNewPasswordLabel')}</Label>
        <Input
          id="confirmNewPassword"
          type="password"
          autoComplete="new-password"
          aria-invalid={!!errors.confirmNewPassword}
          {...register('confirmNewPassword')}
        />
        {errors.confirmNewPassword && (
          <p className="text-xs text-destructive">{errors.confirmNewPassword.message}</p>
        )}
      </div>
      <Button type="submit" loading={isSubmitting} className="self-start">
        {t('auth.changePasswordButton')}
      </Button>
    </form>
  )
}
