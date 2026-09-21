import { useState } from 'react'
import { useForm, useWatch, Controller } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useTranslation } from 'react-i18next'
import { useQuery } from '@tanstack/react-query'
import { toast } from 'sonner'
import { createMaterial } from '@/features/materials/api'
import { getAllCoursesForLookup } from '@/features/courses/api'
import { ApiError } from '@/api/errors'
import { applyServerValidation } from '@/lib/applyServerValidation'
import { enumLabel } from '@/lib/enumLabels'
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
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Field } from '@/components/shared/Field'

const TYPES = ['Video', 'Article', 'Link'] as const

const schema = z.object({
  courseId: z.string().uuid(),
  type: z.enum(TYPES),
  title: z.string().min(1),
  description: z.string().optional(),
  url: z.string().optional(),
  body: z.string().optional(),
})
type FormValues = z.infer<typeof schema>

interface CreateMaterialDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  fixedCourseId?: string
  onCreated: () => void
}

export function CreateMaterialDialog({
  open,
  onOpenChange,
  fixedCourseId,
  onCreated,
}: CreateMaterialDialogProps) {
  const { t } = useTranslation()
  const [formError, setFormError] = useState<string | null>(null)

  const { data: courses } = useQuery({
    queryKey: ['courses', 'lookup-all'],
    queryFn: getAllCoursesForLookup,
    enabled: !fixedCourseId,
    staleTime: 5 * 60_000,
  })

  const {
    register,
    control,
    handleSubmit,
    reset,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { courseId: fixedCourseId ?? '', type: 'Link' },
  })
  const type = useWatch({ control, name: 'type' })

  function handleClose(next: boolean) {
    if (!next) {
      reset({ courseId: fixedCourseId ?? '', type: 'Link' })
      setFormError(null)
    }
    onOpenChange(next)
  }

  async function onSubmit(values: FormValues) {
    setFormError(null)
    try {
      await createMaterial({
        courseId: values.courseId,
        type: values.type,
        title: values.title,
        description: values.description || null,
        body: values.body || null,
        url: values.url || null,
      })
      toast.success(t('materials.createDialog.success'))
      onCreated()
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
          <DialogTitle>{t('materials.createDialog.title')}</DialogTitle>
        </DialogHeader>
        <form className="flex flex-col gap-4" onSubmit={handleSubmit(onSubmit)} noValidate>
          {formError && <p className="text-sm text-destructive">{formError}</p>}

          {!fixedCourseId && (
            <Field label={t('materials.createDialog.courseLabel')} error={errors.courseId?.message}>
              <Controller
                control={control}
                name="courseId"
                render={({ field }) => (
                  <Select value={field.value} onValueChange={field.onChange}>
                    <SelectTrigger aria-invalid={!!errors.courseId}>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {courses?.map((c) => (
                        <SelectItem key={c.id} value={c.id}>
                          {c.name}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                )}
              />
            </Field>
          )}

          <Field label={t('materials.createDialog.typeLabel')}>
            <Controller
              control={control}
              name="type"
              render={({ field }) => (
                <Select value={field.value} onValueChange={field.onChange}>
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {TYPES.map((tp) => (
                      <SelectItem key={tp} value={tp}>
                        {enumLabel(t, 'materialType', tp)}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              )}
            />
          </Field>

          <Field label={t('materials.createDialog.titleLabel')} error={errors.title?.message}>
            <Input {...register('title')} aria-invalid={!!errors.title} />
          </Field>

          <Field label={t('materials.createDialog.descriptionLabel')}>
            <Textarea rows={2} {...register('description')} />
          </Field>

          {(type === 'Video' || type === 'Link') && (
            <Field
              label={t('materials.createDialog.urlLabel')}
              hint={type === 'Video' ? t('materials.createDialog.urlHintVideo') : undefined}
              error={errors.url?.message}
            >
              <Input {...register('url')} aria-invalid={!!errors.url} />
            </Field>
          )}

          {type === 'Article' && (
            <Field label={t('materials.createDialog.bodyLabel')}>
              <Textarea rows={6} {...register('body')} />
            </Field>
          )}

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
              {t('materials.createDialog.submit')}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
