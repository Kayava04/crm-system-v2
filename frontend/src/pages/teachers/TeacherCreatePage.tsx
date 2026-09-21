import { useMemo, useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import { toast } from 'sonner'
import { createTeacher } from '@/features/teachers/api'
import { ApiError } from '@/api/errors'
import { applyServerValidation } from '@/lib/applyServerValidation'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Field } from '@/components/shared/Field'

export function TeacherCreatePage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const [formError, setFormError] = useState<string | null>(null)

  const schema = useMemo(
    () =>
      z.object({
        firstName: z.string().min(1),
        lastName: z.string().min(1),
        middleName: z.string().optional(),
        dateOfBirth: z.string().min(1),
        phoneNumber: z.string().min(1),
        email: z.string().email(),
        city: z.string().min(1),
        country: z.string().min(1),
        baseSalary: z.coerce.number().min(0),
        lessonsRate: z.coerce.number().min(0),
        comment: z.string().optional(),
      }),
    [],
  )
  type FormValues = z.infer<typeof schema>

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { baseSalary: 0, lessonsRate: 0 },
  })

  async function onSubmit(values: FormValues) {
    setFormError(null)
    try {
      const created = await createTeacher({
        ...values,
        middleName: values.middleName || null,
        comment: values.comment || null,
      })
      toast.success(t('teachers.create.success'))
      navigate(`/teachers/${created.id}`)
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
    <div className="mx-auto flex w-full max-w-2xl flex-col gap-6">
      <h1 className="text-2xl font-semibold tracking-tight">{t('teachers.create.title')}</h1>
      <form onSubmit={handleSubmit(onSubmit)} noValidate className="flex flex-col gap-6">
        {formError && (
          <Alert variant="destructive">
            <AlertDescription>{formError}</AlertDescription>
          </Alert>
        )}

        <Card>
          <CardHeader>
            <CardTitle className="text-base font-medium">
              {t('teachers.create.sectionPersonal')}
            </CardTitle>
          </CardHeader>
          <CardContent className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <Field label={t('profile.contact.firstNameLabel')} error={errors.firstName?.message}>
              <Input {...register('firstName')} aria-invalid={!!errors.firstName} />
            </Field>
            <Field label={t('profile.contact.lastNameLabel')} error={errors.lastName?.message}>
              <Input {...register('lastName')} aria-invalid={!!errors.lastName} />
            </Field>
            <Field
              label={`${t('common.middleName')} (${t('common.optional')})`}
              error={errors.middleName?.message}
            >
              <Input {...register('middleName')} />
            </Field>
            <Field label={t('profile.teacherData.dateOfBirth')} error={errors.dateOfBirth?.message}>
              <Input type="date" {...register('dateOfBirth')} aria-invalid={!!errors.dateOfBirth} />
            </Field>
            <Field label={t('profile.contact.phoneLabel')} error={errors.phoneNumber?.message}>
              <Input type="tel" {...register('phoneNumber')} aria-invalid={!!errors.phoneNumber} />
            </Field>
            <Field label={t('profile.teacherData.email')} error={errors.email?.message}>
              <Input type="email" {...register('email')} aria-invalid={!!errors.email} />
            </Field>
            <Field label={t('profile.teacherData.city')} error={errors.city?.message}>
              <Input {...register('city')} aria-invalid={!!errors.city} />
            </Field>
            <Field label={t('profile.teacherData.country')} error={errors.country?.message}>
              <Input {...register('country')} aria-invalid={!!errors.country} />
            </Field>
            <div className="sm:col-span-2">
              <Field label={t('teachers.detail.commentLabel')}>
                <Textarea
                  {...register('comment')}
                  placeholder={t('teachers.detail.commentPlaceholder')}
                />
              </Field>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="text-base font-medium">
              {t('teachers.create.sectionSalary')}
            </CardTitle>
          </CardHeader>
          <CardContent className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <Field label={t('profile.teacherData.baseSalary')} error={errors.baseSalary?.message}>
              <Input
                type="number"
                min={0}
                step="0.01"
                {...register('baseSalary')}
                aria-invalid={!!errors.baseSalary}
              />
            </Field>
            <Field label={t('profile.teacherData.lessonsRate')} error={errors.lessonsRate?.message}>
              <Input
                type="number"
                min={0}
                step="0.01"
                {...register('lessonsRate')}
                aria-invalid={!!errors.lessonsRate}
              />
            </Field>
          </CardContent>
        </Card>

        <div className="flex gap-2">
          <Button type="submit" loading={isSubmitting}>
            {t('teachers.create.submit')}
          </Button>
          <Button type="button" variant="outline" onClick={() => navigate('/teachers')}>
            {t('common.cancel')}
          </Button>
        </div>
      </form>
    </div>
  )
}
