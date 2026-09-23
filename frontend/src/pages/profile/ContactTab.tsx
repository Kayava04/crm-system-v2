import { useMemo, useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useTranslation } from 'react-i18next'
import { useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { useAuth } from '@/features/auth/useAuth'
import { updateMyContact } from '@/features/auth/api'
import { ApiError } from '@/api/errors'
import { applyServerValidation } from '@/lib/applyServerValidation'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Field } from '@/components/shared/Field'
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
        middleName: z.string().optional(),
        phoneNumber: z.string().min(1, t('profile.contact.phoneLabel')),
        dateOfBirth: z.string().optional(),
        city: z.string().optional(),
        country: z.string().optional(),
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
      middleName: user?.contact?.middleName ?? '',
      phoneNumber: user?.contact?.phoneNumber ?? '',
      dateOfBirth: user?.contact?.dateOfBirth ?? '',
      city: user?.contact?.city ?? '',
      country: user?.contact?.country ?? '',
    },
  })

  async function onSubmit(values: FormValues) {
    setFormError(null)
    try {
      await updateMyContact({
        firstName: values.firstName,
        lastName: values.lastName,
        phoneNumber: values.phoneNumber,
        middleName: values.middleName || null,
        dateOfBirth: values.dateOfBirth || null,
        city: values.city || null,
        country: values.country || null,
      })
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
          <div className="grid gap-4 sm:grid-cols-2">
            <Field label={t('profile.contact.firstNameLabel')} error={errors.firstName?.message}>
              <Input aria-invalid={!!errors.firstName} {...register('firstName')} />
            </Field>
            <Field label={t('profile.contact.lastNameLabel')} error={errors.lastName?.message}>
              <Input aria-invalid={!!errors.lastName} {...register('lastName')} />
            </Field>
            <Field label={t('profile.contact.middleNameLabel')} error={errors.middleName?.message}>
              <Input {...register('middleName')} />
            </Field>
            <Field label={t('profile.contact.phoneLabel')} error={errors.phoneNumber?.message}>
              <Input type="tel" aria-invalid={!!errors.phoneNumber} {...register('phoneNumber')} />
            </Field>
            <Field label={t('profile.contact.dateOfBirthLabel')} error={errors.dateOfBirth?.message}>
              <Input type="date" {...register('dateOfBirth')} />
            </Field>
            <Field label={t('profile.contact.cityLabel')} error={errors.city?.message}>
              <Input {...register('city')} />
            </Field>
            <Field label={t('profile.contact.countryLabel')} error={errors.country?.message}>
              <Input {...register('country')} />
            </Field>
          </div>
          <Button type="submit" loading={isSubmitting} className="self-start">
            {t('common.save')}
          </Button>
        </form>
      </CardContent>
    </Card>
  )
}
