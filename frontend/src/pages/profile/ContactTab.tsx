import { useMemo, useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useTranslation } from 'react-i18next'
import { useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { useAuth } from '@/features/auth/AuthProvider'
import { updateMyContact } from '@/features/auth/api'
import { ApiError } from '@/api/errors'
import { applyServerValidation } from '@/lib/applyServerValidation'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Alert, AlertDescription } from '@/components/ui/alert'

export function ContactTab() {
  const { t } = useTranslation()
  const { user } = useAuth()
  const queryClient = useQueryClient()
  const [formError, setFormError] = useState<string | null>(null)

  const schema = useMemo(
    () =>
      z.object({
        firstName: z.string().min(1, t('profile.contact.firstNameLabel')),
        lastName: z.string().min(1, t('profile.contact.lastNameLabel')),
        phoneNumber: z.string().min(1, t('profile.contact.phoneLabel')),
      }),
    [t],
  )
  type FormValues = z.infer<typeof schema>

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    values: {
      firstName: user?.contact?.firstName ?? '',
      lastName: user?.contact?.lastName ?? '',
      phoneNumber: user?.contact?.phoneNumber ?? '',
    },
  })

  async function onSubmit(values: FormValues) {
    setFormError(null)
    try {
      await updateMyContact(values)
      await queryClient.invalidateQueries({ queryKey: ['auth', 'me'] })
      toast.success(t('profile.contact.saved'))
    } catch (err) {
      if (err instanceof ApiError) {
        if (err.isValidation) {
          applyServerValidation(setError, err)
          return
        }
        if (err.isConflict) {
          setFormError(err.detail || t('profile.contact.conflict'))
          return
        }
        setFormError(err.detail || t('common.unknownError'))
        return
      }
      setFormError(t('common.networkError'))
    }
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-base font-medium">{t('profile.tabs.contact')}</CardTitle>
        <CardDescription>{t('profile.contact.description')}</CardDescription>
      </CardHeader>
      <CardContent>
        <form className="flex flex-col gap-4" onSubmit={handleSubmit(onSubmit)} noValidate>
          {formError && (
            <Alert variant="destructive">
              <AlertDescription>{formError}</AlertDescription>
            </Alert>
          )}
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="firstName">{t('profile.contact.firstNameLabel')}</Label>
            <Input id="firstName" aria-invalid={!!errors.firstName} {...register('firstName')} />
            {errors.firstName && (
              <p className="text-xs text-destructive">{errors.firstName.message}</p>
            )}
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="lastName">{t('profile.contact.lastNameLabel')}</Label>
            <Input id="lastName" aria-invalid={!!errors.lastName} {...register('lastName')} />
            {errors.lastName && (
              <p className="text-xs text-destructive">{errors.lastName.message}</p>
            )}
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="phoneNumber">{t('profile.contact.phoneLabel')}</Label>
            <Input
              id="phoneNumber"
              type="tel"
              aria-invalid={!!errors.phoneNumber}
              {...register('phoneNumber')}
            />
            {errors.phoneNumber && (
              <p className="text-xs text-destructive">{errors.phoneNumber.message}</p>
            )}
          </div>
          <Button type="submit" loading={isSubmitting} className="self-start">
            {t('common.save')}
          </Button>
        </form>
      </CardContent>
    </Card>
  )
}
