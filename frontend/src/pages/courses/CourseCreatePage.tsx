import { useMemo, useState } from 'react'
import { useForm, Controller } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import { toast } from 'sonner'
import { createCourse } from '@/features/courses/api'
import { ApiError } from '@/api/errors'
import { applyServerValidation } from '@/lib/applyServerValidation'
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
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Field } from '@/components/shared/Field'

const LANGUAGES = ['English', 'French', 'German', 'Polish', 'Spanish', 'Italian'] as const
const LEVELS = ['A1', 'A2', 'B1', 'B2', 'C1', 'C2'] as const

export function CourseCreatePage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const [formError, setFormError] = useState<string | null>(null)

  const schema = useMemo(
    () =>
      z.object({
        name: z.string().min(1),
        language: z.enum(['English', 'French', 'German', 'Polish', 'Spanish', 'Italian']),
        level: z.enum(['A1', 'A2', 'B1', 'B2', 'C1', 'C2']),
        format: z.enum(['Online', 'Offline']),
        lessonType: z.enum(['Individual', 'Group']),
        durationMonths: z.coerce.number().int().min(1),
        lessonsCount: z.coerce.number().int().min(1),
        lessonsPerWeek: z.coerce.number().int().min(1),
        price: z.coerce.number().min(0),
        description: z.string().optional(),
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
      language: 'English',
      level: 'A1',
      format: 'Online',
      lessonType: 'Individual',
      durationMonths: 3,
      lessonsCount: 24,
      lessonsPerWeek: 2,
      price: 0,
    },
  })

  async function onSubmit(values: FormValues) {
    setFormError(null)
    try {
      const created = await createCourse({ ...values, description: values.description || null })
      toast.success(t('courses.create.success'))
      navigate(`/courses/${created.id}`)
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
    <div className="flex max-w-2xl flex-col gap-6">
      <h1 className="text-2xl font-semibold tracking-tight">{t('courses.create.title')}</h1>
      <form onSubmit={handleSubmit(onSubmit)} noValidate className="flex flex-col gap-6">
        {formError && (
          <Alert variant="destructive">
            <AlertDescription>{formError}</AlertDescription>
          </Alert>
        )}

        <Card>
          <CardHeader>
            <CardTitle className="text-base font-medium">
              {t('courses.create.sectionDetails')}
            </CardTitle>
          </CardHeader>
          <CardContent className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <div className="sm:col-span-2">
              <Field label={t('courses.fields.name')} error={errors.name?.message}>
                <Input {...register('name')} aria-invalid={!!errors.name} />
              </Field>
            </div>
            <Field label={t('courses.fields.language')}>
              <Controller
                control={control}
                name="language"
                render={({ field }) => (
                  <Select value={field.value} onValueChange={field.onChange}>
                    <SelectTrigger>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {LANGUAGES.map((v) => (
                        <SelectItem key={v} value={v}>
                          {t(`enums.language.${v}`)}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                )}
              />
            </Field>
            <Field label={t('courses.fields.level')}>
              <Controller
                control={control}
                name="level"
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
            <Field label={t('courses.fields.format')}>
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
            <Field label={t('courses.fields.lessonType')} hint={t('courses.fields.lessonTypeHint')}>
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
            <Field
              label={t('courses.fields.durationMonths')}
              error={errors.durationMonths?.message}
            >
              <Input
                type="number"
                min={1}
                {...register('durationMonths')}
                aria-invalid={!!errors.durationMonths}
              />
            </Field>
            <Field label={t('courses.fields.lessonsCount')} error={errors.lessonsCount?.message}>
              <Input
                type="number"
                min={1}
                {...register('lessonsCount')}
                aria-invalid={!!errors.lessonsCount}
              />
            </Field>
            <Field
              label={t('courses.fields.lessonsPerWeek')}
              error={errors.lessonsPerWeek?.message}
            >
              <Input
                type="number"
                min={1}
                {...register('lessonsPerWeek')}
                aria-invalid={!!errors.lessonsPerWeek}
              />
            </Field>
            <Field label={t('courses.fields.price')} error={errors.price?.message}>
              <Input
                type="number"
                min={0}
                step="0.01"
                {...register('price')}
                aria-invalid={!!errors.price}
              />
            </Field>
            <div className="sm:col-span-2">
              <Field label={t('courses.fields.description')}>
                <Textarea {...register('description')} />
              </Field>
            </div>
          </CardContent>
        </Card>

        <div className="flex gap-2">
          <Button type="submit" loading={isSubmitting}>
            {t('courses.create.submit')}
          </Button>
          <Button type="button" variant="outline" onClick={() => navigate('/courses')}>
            {t('common.cancel')}
          </Button>
        </div>
      </form>
    </div>
  )
}
