import { useMemo, useState } from 'react'
import { useForm, Controller } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { toast } from 'sonner'
import { createGroup } from '@/features/studyGroups/api'
import { getAllCoursesForLookup } from '@/features/courses/api'
import { ApiError } from '@/api/errors'
import { applyServerValidation } from '@/lib/applyServerValidation'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { Card, CardContent } from '@/components/ui/card'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Field } from '@/components/shared/Field'
import { TeacherSearchInput } from '@/components/shared/TeacherSearchInput'

const schema = z.object({
  name: z.string().min(1),
  courseId: z.string().uuid(),
})
type FormValues = z.infer<typeof schema>

export function StudyGroupCreatePage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const [formError, setFormError] = useState<string | null>(null)
  const [teacher, setTeacher] = useState<{ id: string; fullName: string } | null>(null)
  const [teacherError, setTeacherError] = useState<string | null>(null)

  const { data: allCourses } = useQuery({
    queryKey: ['courses', 'lookup-all'],
    queryFn: getAllCoursesForLookup,
    staleTime: 5 * 60_000,
  })
  // A study group can only be built on a course whose lessonType is Group -
  // picking an Individual-lesson course here is what was producing the opaque
  // 409 on submit (root-caused after reading the backend's group-creation flow).
  const courses = (allCourses ?? []).filter(
    (c) => c.lessonType === 'Group' && c.status === 'Active',
  )

  const {
    register,
    control,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({ resolver: zodResolver(schema) })

  const submitLabel = useMemo(() => t('studyGroups.create.submit'), [t])

  async function onSubmit(values: FormValues) {
    setFormError(null)
    if (!teacher) {
      setTeacherError(t('common.required'))
      return
    }
    setTeacherError(null)
    try {
      const created = await createGroup({
        name: values.name,
        courseId: values.courseId,
        teacherId: teacher.id,
      })
      toast.success(t('studyGroups.create.success'))
      navigate(`/study-groups/${created.id}`)
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
    <div className="mx-auto flex w-full max-w-lg flex-col gap-6">
      <h1 className="text-2xl font-semibold tracking-tight">{t('studyGroups.create.title')}</h1>
      <form onSubmit={handleSubmit(onSubmit)} noValidate className="flex flex-col gap-6">
        {formError && (
          <Alert variant="destructive">
            <AlertDescription>{formError}</AlertDescription>
          </Alert>
        )}
        <Card>
          <CardContent className="flex flex-col gap-4 pt-6">
            <Field label={t('studyGroups.create.nameLabel')} error={errors.name?.message}>
              <Input {...register('name')} aria-invalid={!!errors.name} />
            </Field>
            <Field label={t('studyGroups.create.courseLabel')} error={errors.courseId?.message}>
              <Controller
                control={control}
                name="courseId"
                render={({ field }) => (
                  <Select
                    value={field.value}
                    onValueChange={field.onChange}
                    disabled={courses.length === 0}
                  >
                    <SelectTrigger aria-invalid={!!errors.courseId}>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {courses.map((c) => (
                        <SelectItem key={c.id} value={c.id}>
                          {c.name}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                )}
              />
              {courses.length === 0 && (
                <p className="text-xs text-muted-foreground">
                  {t('studyGroups.create.noGroupCourses')}
                </p>
              )}
            </Field>
            <Field label={t('studyGroups.create.teacherLabel')} error={teacherError ?? undefined}>
              <TeacherSearchInput value={teacher} onChange={setTeacher} />
            </Field>
          </CardContent>
        </Card>

        <div className="flex gap-2">
          <Button type="submit" loading={isSubmitting}>
            {submitLabel}
          </Button>
          <Button type="button" variant="outline" onClick={() => navigate('/study-groups')}>
            {t('common.cancel')}
          </Button>
        </div>
      </form>
    </div>
  )
}
