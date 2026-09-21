import { useMemo, useState } from 'react'
import { useForm, Controller } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import { toast } from 'sonner'
import { createStudent } from '@/features/students/api'
import { ApiError } from '@/api/errors'
import { applyServerValidation } from '@/lib/applyServerValidation'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import { Checkbox } from '@/components/ui/checkbox'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Field } from '@/components/shared/Field'

const LANGUAGES = ['English', 'French', 'German', 'Polish', 'Spanish', 'Italian'] as const
const LEVELS = ['A1', 'A2', 'B1', 'B2', 'C1', 'C2'] as const

export function StudentCreatePage() {
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
        isChild: z.boolean(),
        comment: z.string().optional(),
        learningGoal: z.enum(['Work', 'Relocation', 'Study', 'Personal']),
        format: z.enum(['Online', 'Offline']),
        lessonType: z.enum(['Individual', 'Group']),
        intensity: z.coerce.number().int().min(1),
        currentLevel: z.enum(['A1', 'A2', 'B1', 'B2', 'C1', 'C2']),
        hadPreviousCourses: z.boolean(),
        languages: z.array(z.enum(['English', 'French', 'German', 'Polish', 'Spanish', 'Italian'])),
      }),
    [],
  )
  type FormValues = z.infer<typeof schema>

  const {
    register,
    control,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      isChild: false,
      hadPreviousCourses: false,
      languages: [],
      format: 'Online',
      lessonType: 'Individual',
      learningGoal: 'Study',
      currentLevel: 'A1',
      intensity: 2,
    },
  })

  async function onSubmit(values: FormValues) {
    setFormError(null)
    try {
      const created = await createStudent({
        ...values,
        middleName: values.middleName || null,
        comment: values.comment || null,
      })
      toast.success(t('students.create.success'))
      navigate(`/students/${created.id}`)
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
      <h1 className="text-2xl font-semibold tracking-tight">{t('students.create.title')}</h1>
      <form onSubmit={handleSubmit(onSubmit)} noValidate className="flex flex-col gap-6">
        {formError && (
          <Alert variant="destructive">
            <AlertDescription>{formError}</AlertDescription>
          </Alert>
        )}

        <Card>
          <CardHeader>
            <CardTitle className="text-base font-medium">
              {t('students.create.sectionPersonal')}
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
            <Field label={t('profile.studentData.dateOfBirth')} error={errors.dateOfBirth?.message}>
              <Input type="date" {...register('dateOfBirth')} aria-invalid={!!errors.dateOfBirth} />
            </Field>
            <Field label={t('profile.contact.phoneLabel')} error={errors.phoneNumber?.message}>
              <Input type="tel" {...register('phoneNumber')} aria-invalid={!!errors.phoneNumber} />
            </Field>
            <Field label={t('profile.studentData.email')} error={errors.email?.message}>
              <Input type="email" {...register('email')} aria-invalid={!!errors.email} />
            </Field>
            <Field label={t('profile.studentData.city')} error={errors.city?.message}>
              <Input {...register('city')} aria-invalid={!!errors.city} />
            </Field>
            <Field label={t('profile.studentData.country')} error={errors.country?.message}>
              <Input {...register('country')} aria-invalid={!!errors.country} />
            </Field>
            <div className="flex items-center gap-2 pt-6">
              <Controller
                control={control}
                name="isChild"
                render={({ field }) => (
                  <Checkbox
                    checked={field.value}
                    onCheckedChange={(v) => field.onChange(v === true)}
                    id="isChild"
                  />
                )}
              />
              <Label htmlFor="isChild" className="font-normal">
                {t('profile.studentData.isChild')}
              </Label>
            </div>
            <div className="sm:col-span-2">
              <Field label={t('students.detail.commentLabel')}>
                <Textarea
                  {...register('comment')}
                  placeholder={t('students.detail.commentPlaceholder')}
                />
              </Field>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="text-base font-medium">
              {t('students.create.sectionPreferences')}
            </CardTitle>
          </CardHeader>
          <CardContent className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <Field label={t('profile.studentData.learningGoal')}>
              <Controller
                control={control}
                name="learningGoal"
                render={({ field }) => (
                  <Select value={field.value} onValueChange={field.onChange}>
                    <SelectTrigger>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {(['Work', 'Relocation', 'Study', 'Personal'] as const).map((v) => (
                        <SelectItem key={v} value={v}>
                          {t(`enums.learningGoal.${v}`)}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                )}
              />
            </Field>
            <Field label={t('profile.studentData.format')}>
              <Controller
                control={control}
                name="format"
                render={({ field }) => (
                  <Select value={field.value} onValueChange={field.onChange}>
                    <SelectTrigger>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {(['Online', 'Offline'] as const).map((v) => (
                        <SelectItem key={v} value={v}>
                          {t(`enums.format.${v}`)}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                )}
              />
            </Field>
            <Field label={t('profile.studentData.lessonType')}>
              <Controller
                control={control}
                name="lessonType"
                render={({ field }) => (
                  <Select value={field.value} onValueChange={field.onChange}>
                    <SelectTrigger>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {(['Individual', 'Group'] as const).map((v) => (
                        <SelectItem key={v} value={v}>
                          {t(`enums.lessonType.${v}`)}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                )}
              />
            </Field>
            <Field label={t('profile.studentData.intensity')} error={errors.intensity?.message}>
              <Input
                type="number"
                min={1}
                {...register('intensity')}
                aria-invalid={!!errors.intensity}
              />
            </Field>
            <Field label={t('profile.studentData.currentLevel')}>
              <Controller
                control={control}
                name="currentLevel"
                render={({ field }) => (
                  <Select value={field.value} onValueChange={field.onChange}>
                    <SelectTrigger>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {LEVELS.map((v) => (
                        <SelectItem key={v} value={v}>
                          {v}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                )}
              />
            </Field>
            <div className="flex items-center gap-2 pt-6">
              <Controller
                control={control}
                name="hadPreviousCourses"
                render={({ field }) => (
                  <Checkbox
                    checked={field.value}
                    onCheckedChange={(v) => field.onChange(v === true)}
                    id="hadPreviousCourses"
                  />
                )}
              />
              <Label htmlFor="hadPreviousCourses" className="font-normal">
                {t('profile.studentData.hadPreviousCourses')}
              </Label>
            </div>
            <div className="sm:col-span-2">
              <Label className="mb-2 block">{t('profile.studentData.languages')}</Label>
              <Controller
                control={control}
                name="languages"
                render={({ field }) => (
                  <div className="flex flex-wrap gap-3">
                    {LANGUAGES.map((lang) => (
                      <label key={lang} className="flex items-center gap-1.5 text-sm">
                        <Checkbox
                          checked={field.value.includes(lang)}
                          onCheckedChange={(checked) => {
                            field.onChange(
                              checked
                                ? [...field.value, lang]
                                : field.value.filter((l) => l !== lang),
                            )
                          }}
                        />
                        {t(`enums.language.${lang}`)}
                      </label>
                    ))}
                  </div>
                )}
              />
            </div>
          </CardContent>
        </Card>

        <div className="flex gap-2">
          <Button type="submit" loading={isSubmitting}>
            {t('students.create.submit')}
          </Button>
          <Button type="button" variant="outline" onClick={() => navigate('/students')}>
            {t('common.cancel')}
          </Button>
        </div>
      </form>
    </div>
  )
}
