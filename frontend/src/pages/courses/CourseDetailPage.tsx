import { useMemo, useState } from 'react'
import { useForm, Controller } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { ArrowLeft, Pencil, Archive, CheckCircle2, Trash2, UserPlus } from 'lucide-react'
import {
  getCourseById,
  updateCourse,
  activateCourse,
  archiveCourse,
  deleteCourse,
} from '@/features/courses/api'
import { getEnrollments } from '@/features/enrollments/api'
import { useCourseTeachers } from '@/features/courses/useCourseTeachers'
import { ApiError } from '@/api/errors'
import { applyServerValidation } from '@/lib/applyServerValidation'
import { enumLabel } from '@/lib/enumLabels'
import { useCan } from '@/features/auth/useCan'
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
import { Badge } from '@/components/ui/badge'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { Skeleton } from '@/components/ui/skeleton'
import { ConfirmDialog } from '@/components/shared/ConfirmDialog'
import { Field } from '@/components/shared/Field'
import { EnrollmentsTable } from '@/components/shared/EnrollmentsTable'
import { CreateEnrollmentDialog } from '@/components/shared/CreateEnrollmentDialog'

const LANGUAGES = ['English', 'French', 'German', 'Polish', 'Spanish', 'Italian'] as const
const LEVELS = ['A1', 'A2', 'B1', 'B2', 'C1', 'C2'] as const

function statusVariant(status: string): 'success' | 'secondary' {
  return status === 'Active' ? 'success' : 'secondary'
}

export function CourseDetailPage() {
  const { id = '' } = useParams()
  const { t } = useTranslation()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const canManage = useCan('CanManageCourses')
  const canManageEnrollments = useCan('CanManageEnrollments')

  const [activeTab, setActiveTab] = useState('profile')
  const [editing, setEditing] = useState(false)
  const [archiveOpen, setArchiveOpen] = useState(false)
  const [activateOpen, setActivateOpen] = useState(false)
  const [deleteOpen, setDeleteOpen] = useState(false)
  const [enrollOpen, setEnrollOpen] = useState(false)

  const { data: course, isLoading } = useQuery({
    queryKey: ['courses', id],
    queryFn: () => getCourseById(id),
    enabled: !!id,
  })
  const { teachers, isLoading: teachersLoading } = useCourseTeachers(id)

  const { data: enrollments } = useQuery({
    queryKey: ['enrollments', { courseId: id }],
    queryFn: () => getEnrollments({ courseId: id, pageSize: 50 }),
    enabled: !!id && activeTab === 'enrollments',
  })

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
    reset,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    values: course
      ? {
          name: course.name,
          language: course.language as FormValues['language'],
          level: course.level as FormValues['level'],
          format: (course.format ?? 'Online') as FormValues['format'],
          lessonType: course.lessonType,
          durationMonths: Number(course.durationMonths),
          lessonsCount: Number(course.lessonsCount),
          lessonsPerWeek: Number(course.lessonsPerWeek),
          price: Number(course.price),
          description: course.description ?? '',
        }
      : undefined,
  })

  const archiveMutation = useMutation({
    mutationFn: () => archiveCourse(id),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['courses', id] })
      await queryClient.invalidateQueries({ queryKey: ['courses'] })
      toast.success(t('courses.detail.archive'))
    },
    onError: (err) => {
      toast.error(
        err instanceof ApiError ? err.detail || t('common.unknownError') : t('common.networkError'),
      )
    },
  })

  const activateMutation = useMutation({
    mutationFn: () => activateCourse(id),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['courses', id] })
      await queryClient.invalidateQueries({ queryKey: ['courses'] })
      toast.success(t('courses.detail.activate'))
    },
    onError: (err) => {
      toast.error(
        err instanceof ApiError ? err.detail || t('common.unknownError') : t('common.networkError'),
      )
    },
  })

  const deleteMutation = useMutation({
    mutationFn: () => deleteCourse(id),
    onSuccess: () => {
      toast.success(t('courses.detail.delete'))
      navigate('/courses')
    },
    onError: (err) => {
      toast.error(
        err instanceof ApiError ? err.detail || t('common.unknownError') : t('common.networkError'),
      )
    },
  })

  async function onSubmit(values: FormValues) {
    try {
      await updateCourse(id, { ...values, description: values.description || null })
      await queryClient.invalidateQueries({ queryKey: ['courses', id] })
      await queryClient.invalidateQueries({ queryKey: ['courses'] })
      toast.success(t('courses.detail.profileSaved'))
      setEditing(false)
    } catch (err) {
      if (err instanceof ApiError && err.isValidation) {
        applyServerValidation(setError, err)
        return
      }
      toast.error(
        err instanceof ApiError ? err.detail || t('common.unknownError') : t('common.networkError'),
      )
    }
  }

  if (isLoading || !course) {
    return (
      <div className="mx-auto flex w-full max-w-3xl flex-col gap-4">
        <Skeleton className="h-8 w-64" />
        <Skeleton className="h-40 w-full" />
      </div>
    )
  }

  return (
    <div className="mx-auto flex w-full max-w-3xl flex-col gap-6">
      <div className="flex flex-col gap-4">
        <Link
          to="/courses"
          className="flex w-fit items-center gap-1 text-sm text-muted-foreground hover:text-foreground"
        >
          <ArrowLeft className="size-4" />
          {t('courses.detail.backToList')}
        </Link>

        <div className="flex flex-col gap-3">
          <div className="flex flex-col gap-1">
            <h1 className="text-2xl font-semibold tracking-tight">{course.name}</h1>
            <div className="flex flex-wrap items-center gap-2">
              <Badge variant={statusVariant(course.status)}>
                {enumLabel(t, 'courseStatus', course.status)}
              </Badge>
              <span className="text-sm text-muted-foreground">
                {enumLabel(t, 'language', course.language)} · {enumLabel(t, 'level', course.level)}
              </span>
            </div>
          </div>

          {canManage && (
            <div className="flex flex-wrap justify-end gap-2">
              {course.status === 'Active' ? (
                <Button variant="outline" onClick={() => setArchiveOpen(true)}>
                  <Archive />
                  {t('courses.detail.archive')}
                </Button>
              ) : (
                <Button variant="outline" onClick={() => setActivateOpen(true)}>
                  <CheckCircle2 />
                  {t('courses.detail.activate')}
                </Button>
              )}
              <Button variant="outline" onClick={() => setDeleteOpen(true)}>
                <Trash2 />
                {t('courses.detail.delete')}
              </Button>
            </div>
          )}
        </div>
      </div>

      <Tabs value={activeTab} onValueChange={setActiveTab}>
        <TabsList>
          <TabsTrigger value="profile">{t('courses.detail.tabs.profile')}</TabsTrigger>
          <TabsTrigger value="enrollments">{t('courses.detail.tabs.enrollments')}</TabsTrigger>
        </TabsList>

        <TabsContent value="profile">
          {!editing ? (
            <Card>
              <CardHeader className="flex flex-row items-center justify-between">
                <CardTitle className="text-base font-medium">
                  {t('courses.detail.tabs.profile')}
                </CardTitle>
                {canManage && (
                  <Button variant="outline" size="sm" onClick={() => setEditing(true)}>
                    <Pencil />
                    {t('courses.detail.edit')}
                  </Button>
                )}
              </CardHeader>
              <CardContent className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                <ReadField label={t('courses.fields.name')} value={course.name} />
                <ReadField
                  label={t('courses.fields.language')}
                  value={enumLabel(t, 'language', course.language)}
                />
                <ReadField
                  label={t('courses.fields.level')}
                  value={enumLabel(t, 'level', course.level)}
                />
                <ReadField
                  label={t('courses.fields.format')}
                  value={enumLabel(t, 'format', course.format)}
                />
                <ReadField
                  label={t('courses.fields.lessonType')}
                  value={enumLabel(t, 'lessonType', course.lessonType)}
                />
                <ReadField
                  label={t('courses.fields.teachers')}
                  value={
                    teachersLoading
                      ? '…'
                      : teachers.length > 0
                        ? teachers.map((tch) => tch.name).join(', ')
                        : '—'
                  }
                />
                <ReadField
                  label={t('courses.fields.durationMonths')}
                  value={String(course.durationMonths)}
                />
                <ReadField
                  label={t('courses.fields.lessonsCount')}
                  value={String(course.lessonsCount)}
                />
                <ReadField
                  label={t('courses.fields.lessonsPerWeek')}
                  value={String(course.lessonsPerWeek)}
                />
                <ReadField label={t('courses.fields.price')} value={String(course.price)} />
                <div className="sm:col-span-2">
                  <ReadField
                    label={t('courses.fields.description')}
                    value={course.description || '—'}
                  />
                </div>
              </CardContent>
            </Card>
          ) : (
            <Card>
              <CardHeader>
                <CardTitle className="text-base font-medium">{t('courses.detail.edit')}</CardTitle>
              </CardHeader>
              <CardContent>
                <form
                  className="grid grid-cols-1 gap-4 sm:grid-cols-2"
                  onSubmit={handleSubmit(onSubmit)}
                  noValidate
                >
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
                  <Field label={t('courses.fields.lessonType')}>
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
                    <Input type="number" min={1} {...register('durationMonths')} />
                  </Field>
                  <Field
                    label={t('courses.fields.lessonsCount')}
                    error={errors.lessonsCount?.message}
                  >
                    <Input type="number" min={1} {...register('lessonsCount')} />
                  </Field>
                  <Field
                    label={t('courses.fields.lessonsPerWeek')}
                    error={errors.lessonsPerWeek?.message}
                  >
                    <Input type="number" min={1} {...register('lessonsPerWeek')} />
                  </Field>
                  <Field label={t('courses.fields.price')} error={errors.price?.message}>
                    <Input type="number" min={0} step="0.01" {...register('price')} />
                  </Field>
                  <div className="sm:col-span-2">
                    <Field label={t('courses.fields.description')}>
                      <Textarea {...register('description')} />
                    </Field>
                  </div>
                  <div className="flex gap-2 sm:col-span-2">
                    <Button type="submit" loading={isSubmitting}>
                      {t('courses.detail.save')}
                    </Button>
                    <Button
                      type="button"
                      variant="outline"
                      onClick={() => {
                        reset()
                        setEditing(false)
                      }}
                    >
                      {t('courses.detail.cancelEdit')}
                    </Button>
                  </div>
                </form>
              </CardContent>
            </Card>
          )}
        </TabsContent>

        <TabsContent value="enrollments">
          <Card>
            <CardHeader className="flex flex-row items-center justify-between">
              <CardTitle className="text-base font-medium">
                {t('courses.detail.tabs.enrollments')}
              </CardTitle>
              {canManageEnrollments && (
                <Button variant="outline" size="sm" onClick={() => setEnrollOpen(true)}>
                  <UserPlus />
                  {t('courses.detail.enroll')}
                </Button>
              )}
            </CardHeader>
            <CardContent>
              {enrollments && enrollments.items.length > 0 ? (
                <EnrollmentsTable rows={enrollments.items} hideColumn="course" />
              ) : (
                <p className="text-sm text-muted-foreground">{t('courses.detail.noEnrollments')}</p>
              )}
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>

      <ConfirmDialog
        open={archiveOpen}
        onOpenChange={setArchiveOpen}
        title={t('courses.detail.confirmArchiveTitle')}
        description={t('courses.detail.confirmArchiveDesc')}
        destructive
        onConfirm={async () => {
          await archiveMutation.mutateAsync()
        }}
      />
      <ConfirmDialog
        open={activateOpen}
        onOpenChange={setActivateOpen}
        title={t('courses.detail.confirmActivateTitle')}
        description={t('courses.detail.confirmActivateDesc')}
        onConfirm={async () => {
          await activateMutation.mutateAsync()
        }}
      />
      <ConfirmDialog
        open={deleteOpen}
        onOpenChange={setDeleteOpen}
        title={t('courses.detail.confirmDeleteTitle')}
        description={t('courses.detail.confirmDeleteDesc')}
        destructive
        onConfirm={async () => {
          await deleteMutation.mutateAsync()
        }}
      />
      <CreateEnrollmentDialog
        open={enrollOpen}
        onOpenChange={setEnrollOpen}
        fixedCourseId={id}
        fixedCourseLabel={course.name}
        onCreated={() =>
          queryClient.invalidateQueries({ queryKey: ['enrollments', { courseId: id }] })
        }
      />
    </div>
  )
}

function ReadField({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex flex-col gap-1">
      <span className="text-xs text-muted-foreground">{label}</span>
      <span className="text-sm">{value}</span>
    </div>
  )
}
