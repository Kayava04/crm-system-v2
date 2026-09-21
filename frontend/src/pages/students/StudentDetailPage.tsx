import { useMemo, useState } from 'react'
import { useForm, Controller } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { ArrowLeft, Pencil, UserX, UserCheck, Trash2, Plus } from 'lucide-react'
import {
  getStudentById,
  updateStudent,
  updateStudentPreferences,
  updateStudentComment,
  changeStudentStatus,
  deleteStudent,
  addParentInfo,
  updateParentInfo,
  deleteParentInfo,
} from '@/features/students/api'
import { getEnrollments } from '@/features/enrollments/api'
import { getInvoices } from '@/features/billing/api'
import { useResolvedInvoices } from '@/features/billing/useResolvedInvoices'
import { useStudentLessons } from '@/features/scheduling/useStudentLessons'
import { ApiError } from '@/api/errors'
import { applyServerValidation } from '@/lib/applyServerValidation'
import { useAuthenticatedBlobUrl } from '@/lib/useAuthenticatedBlobUrl'
import { enumLabel } from '@/lib/enumLabels'
import { toNum, formatCurrency, formatDate, formatDateTime } from '@/lib/utils'
import { useCan } from '@/features/auth/useCan'
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
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar'
import { Badge } from '@/components/ui/badge'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { Skeleton } from '@/components/ui/skeleton'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { ConfirmDialog } from '@/components/shared/ConfirmDialog'
import { EnrollmentsTable } from '@/components/shared/EnrollmentsTable'
import { DataTable, type DataTableColumn } from '@/components/shared/DataTable'
import { Pagination } from '@/components/shared/Pagination'
import { CreateEnrollmentDialog } from '@/components/shared/CreateEnrollmentDialog'
import { CreateProfileAccountDialog } from '@/components/shared/CreateProfileAccountDialog'
import { TemporaryPasswordDialog } from '@/components/shared/TemporaryPasswordDialog'
import { Field } from '@/components/shared/Field'

const LANGUAGES = ['English', 'French', 'German', 'Polish', 'Spanish', 'Italian'] as const
const LEVELS = ['A1', 'A2', 'B1', 'B2', 'C1', 'C2'] as const
const INACTIVE_STATUSES = ['Suspended', 'Graduated', 'Withdrawn'] as const

function statusVariant(status: string): 'success' | 'warning' | 'secondary' | 'destructive' {
  if (status === 'Active') return 'success'
  if (status === 'Suspended') return 'warning'
  if (status === 'Graduated') return 'secondary'
  return 'destructive'
}

export function StudentDetailPage() {
  const { id = '' } = useParams()
  const { t } = useTranslation()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const canManage = useCan('CanManageStudents')
  const canDelete = useCan('CanDeleteStudents')
  const canCreateAccount = useCan('CanManageAdmins')

  const [activeTab, setActiveTab] = useState<string | null>(null)
  const [deactivateOpen, setDeactivateOpen] = useState(false)
  const [reactivateOpen, setReactivateOpen] = useState(false)
  const [deleteOpen, setDeleteOpen] = useState(false)
  const [deactivateTarget, setDeactivateTarget] =
    useState<(typeof INACTIVE_STATUSES)[number]>('Suspended')
  const [createAccountOpen, setCreateAccountOpen] = useState(false)
  const [createdAccount, setCreatedAccount] = useState<{
    email: string
    temporaryPassword: string
  } | null>(null)

  const { data: student, isLoading } = useQuery({
    queryKey: ['students', id],
    queryFn: () => getStudentById(id),
    enabled: !!id,
  })

  // Defaults to the guardian-info tab for a newly created child student who
  // has none yet, otherwise the profile tab - derived from data rather than
  // synced via an effect. CreateRequest has no parentInfo field at all
  // (confirmed against the backend), so it can only ever be added here, as a
  // second step, and without this default it's easy to miss.
  const effectiveTab = activeTab ?? (student?.isChild && !student.parentInfo ? 'parent' : 'profile')

  const photoUrl = useAuthenticatedBlobUrl(student ? `/api/students/${id}/photo` : null)

  const statusMutation = useMutation({
    mutationFn: (status: 'Active' | 'Suspended' | 'Graduated' | 'Withdrawn') =>
      changeStudentStatus(id, status),
    onSuccess: async (result) => {
      await queryClient.invalidateQueries({ queryKey: ['students', id] })
      await queryClient.invalidateQueries({ queryKey: ['students'] })
      const lines = [
        toNum(result.suspendedEnrollments)
          ? t('students.detail.suspendedEnrollments', { count: toNum(result.suspendedEnrollments) })
          : null,
        toNum(result.cancelledLessons)
          ? t('students.detail.cancelledLessons', { count: toNum(result.cancelledLessons) })
          : null,
        toNum(result.resumedEnrollments)
          ? t('students.detail.resumedEnrollments', { count: toNum(result.resumedEnrollments) })
          : null,
        toNum(result.restoredLessons)
          ? t('students.detail.restoredLessons', { count: toNum(result.restoredLessons) })
          : null,
        toNum(result.lessonsLeftToSchedule)
          ? t('students.detail.lessonsLeftToSchedule', {
              count: toNum(result.lessonsLeftToSchedule),
            })
          : null,
      ].filter(Boolean)
      toast.success(t('students.detail.statusChangeResultTitle'), {
        description: lines.length ? lines.join(' · ') : undefined,
      })
    },
    onError: (err) => {
      toast.error(
        err instanceof ApiError ? err.detail || t('common.unknownError') : t('common.networkError'),
      )
    },
  })

  const deleteMutation = useMutation({
    mutationFn: () => deleteStudent(id),
    onSuccess: () => {
      toast.success(t('students.detail.delete'))
      navigate('/students')
    },
  })

  const [deleteConflict, setDeleteConflict] = useState(false)

  async function handleDelete() {
    setDeleteConflict(false)
    try {
      await deleteMutation.mutateAsync()
    } catch (err) {
      if (err instanceof ApiError && err.isConflict) {
        setDeleteConflict(true)
        return
      }
      toast.error(
        err instanceof ApiError ? err.detail || t('common.unknownError') : t('common.networkError'),
      )
    }
  }

  if (isLoading || !student) {
    return (
      <div className="mx-auto flex w-full max-w-3xl flex-col gap-4">
        <Skeleton className="h-8 w-64" />
        <Skeleton className="h-40 w-full" />
      </div>
    )
  }

  const fullName = [student.lastName, student.firstName, student.middleName]
    .filter(Boolean)
    .join(' ')
  const initials = `${student.firstName[0] ?? ''}${student.lastName[0] ?? ''}`.toUpperCase()

  return (
    <div className="mx-auto flex w-full max-w-3xl flex-col gap-6">
      <div className="flex flex-col gap-4">
        <Link
          to="/students"
          className="flex w-fit items-center gap-1 text-sm text-muted-foreground hover:text-foreground"
        >
          <ArrowLeft className="size-4" />
          {t('students.detail.backToList')}
        </Link>

        <div className="flex flex-wrap items-center gap-4">
          <Avatar className="size-16">
            {photoUrl && <AvatarImage src={photoUrl} alt="" />}
            <AvatarFallback className="text-lg">{initials || '?'}</AvatarFallback>
          </Avatar>
          <div className="flex flex-col gap-1">
            <h1 className="text-2xl font-semibold tracking-tight">{fullName}</h1>
            <div className="flex flex-wrap items-center gap-2">
              <Badge variant={statusVariant(student.status)}>
                {enumLabel(t, 'studentStatus', student.status)}
              </Badge>
              {student.isChild && (
                <Badge variant="outline">{t('profile.studentData.isChild')}</Badge>
              )}
              {student.hasAccount && (
                <Badge variant={student.status === 'Withdrawn' ? 'secondary' : 'success'}>
                  {student.status === 'Withdrawn'
                    ? t('accountLink.accountDeactivated')
                    : t('accountLink.accountActive')}
                </Badge>
              )}
            </div>
          </div>

          <div className="ml-auto flex flex-wrap gap-2">
            {canCreateAccount && !student.hasAccount && (
              <Button variant="outline" onClick={() => setCreateAccountOpen(true)}>
                <UserCheck />
                {t('accountLink.createButton')}
              </Button>
            )}
            {canManage && (
              <>
                {student.status === 'Active' ? (
                  <Button variant="outline" onClick={() => setDeactivateOpen(true)}>
                    <UserX />
                    {t('students.detail.deactivate')}
                  </Button>
                ) : (
                  <Button variant="outline" onClick={() => setReactivateOpen(true)}>
                    <UserCheck />
                    {t('students.detail.reactivate')}
                  </Button>
                )}
                {canDelete && (
                  <Button variant="outline" onClick={() => setDeleteOpen(true)}>
                    <Trash2 />
                    {t('students.detail.delete')}
                  </Button>
                )}
              </>
            )}
          </div>
        </div>
      </div>

      {deleteConflict && (
        <Alert variant="destructive">
          <AlertDescription>
            <strong>{t('students.detail.deleteConflictTitle')}</strong> —{' '}
            {t('students.detail.deleteConflictDesc')}
          </AlertDescription>
        </Alert>
      )}

      <Tabs value={effectiveTab} onValueChange={setActiveTab}>
        <TabsList>
          <TabsTrigger value="profile">{t('students.detail.tabs.profile')}</TabsTrigger>
          <TabsTrigger value="preferences">{t('students.detail.tabs.preferences')}</TabsTrigger>
          {student.isChild && (
            <TabsTrigger value="parent">{t('students.detail.tabs.parent')}</TabsTrigger>
          )}
          <TabsTrigger value="comment">{t('students.detail.tabs.comment')}</TabsTrigger>
          <TabsTrigger value="enrollments">{t('students.detail.tabs.enrollments')}</TabsTrigger>
          <TabsTrigger value="lessons">{t('students.detail.tabs.lessons')}</TabsTrigger>
          <TabsTrigger value="invoices">{t('students.detail.tabs.invoices')}</TabsTrigger>
        </TabsList>

        <TabsContent value="profile">
          <ProfileTab student={student} canManage={canManage} studentId={id} />
        </TabsContent>
        <TabsContent value="preferences">
          <PreferencesTab student={student} canManage={canManage} studentId={id} />
        </TabsContent>
        {student.isChild && (
          <TabsContent value="parent">
            <ParentTab studentId={id} parentInfo={student.parentInfo} canManage={canManage} />
          </TabsContent>
        )}
        <TabsContent value="comment">
          <CommentTab studentId={id} comment={student.comment} canManage={canManage} />
        </TabsContent>
        <TabsContent value="enrollments">
          <EnrollmentsTab studentId={id} studentName={fullName} />
        </TabsContent>
        <TabsContent value="lessons">
          <LessonsTab studentId={id} />
        </TabsContent>
        <TabsContent value="invoices">
          <InvoicesTab studentId={id} />
        </TabsContent>
      </Tabs>

      <Dialog open={deactivateOpen} onOpenChange={setDeactivateOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t('students.detail.confirmDeactivateTitle')}</DialogTitle>
            <DialogDescription>{t('students.detail.confirmDeactivateDesc')}</DialogDescription>
          </DialogHeader>
          <Field label={t('students.detail.newStatusLabel')}>
            <Select
              value={deactivateTarget}
              onValueChange={(v) => setDeactivateTarget(v as typeof deactivateTarget)}
            >
              <SelectTrigger>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {INACTIVE_STATUSES.map((v) => (
                  <SelectItem key={v} value={v}>
                    {enumLabel(t, 'studentStatus', v)}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </Field>
          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setDeactivateOpen(false)}
              disabled={statusMutation.isPending}
            >
              {t('common.cancel')}
            </Button>
            <Button
              variant="destructive"
              loading={statusMutation.isPending}
              onClick={async () => {
                await statusMutation.mutateAsync(deactivateTarget)
                setDeactivateOpen(false)
              }}
            >
              {t('common.confirm')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
      <ConfirmDialog
        open={reactivateOpen}
        onOpenChange={setReactivateOpen}
        title={t('students.detail.confirmReactivateTitle')}
        description={t('students.detail.confirmReactivateDesc')}
        onConfirm={async () => {
          await statusMutation.mutateAsync('Active')
        }}
      />
      <ConfirmDialog
        open={deleteOpen}
        onOpenChange={setDeleteOpen}
        title={t('students.detail.confirmDeleteTitle')}
        description={t('students.detail.confirmDeleteDesc')}
        destructive
        onConfirm={handleDelete}
      />

      <CreateProfileAccountDialog
        open={createAccountOpen}
        onOpenChange={setCreateAccountOpen}
        profileType="Student"
        profileId={id}
        defaultEmail={student.email}
        onCreated={(result) => {
          queryClient.invalidateQueries({ queryKey: ['students', id] })
          setCreatedAccount(result)
        }}
      />
      <TemporaryPasswordDialog
        open={!!createdAccount}
        onOpenChange={(open) => !open && setCreatedAccount(null)}
        email={createdAccount?.email}
        password={createdAccount?.temporaryPassword ?? null}
      />
    </div>
  )
}

type StudentDetailData = NonNullable<
  ReturnType<typeof useQuery<Awaited<ReturnType<typeof getStudentById>>>>['data']
>

function ProfileTab({
  student,
  canManage,
  studentId,
}: {
  student: StudentDetailData
  canManage: boolean
  studentId: string
}) {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const [editing, setEditing] = useState(false)

  const schema = useMemo(
    () =>
      z.object({
        firstName: z.string().min(1),
        lastName: z.string().min(1),
        middleName: z.string().optional(),
        dateOfBirth: z.string().min(1),
        phoneNumber: z.string().min(1),
        city: z.string().min(1),
        country: z.string().min(1),
        isChild: z.boolean(),
      }),
    [],
  )
  type FormValues = z.infer<typeof schema>

  const {
    register,
    control,
    handleSubmit,
    setError,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    values: {
      firstName: student.firstName,
      lastName: student.lastName,
      middleName: student.middleName ?? '',
      dateOfBirth: student.dateOfBirth,
      phoneNumber: student.phoneNumber,
      city: student.city,
      country: student.country,
      isChild: student.isChild,
    },
  })

  async function onSubmit(values: FormValues) {
    try {
      await updateStudent(studentId, { ...values, middleName: values.middleName || null })
      await queryClient.invalidateQueries({ queryKey: ['students', studentId] })
      await queryClient.invalidateQueries({ queryKey: ['students'] })
      toast.success(t('students.detail.profileSaved'))
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

  if (!editing) {
    return (
      <Card>
        <CardHeader className="flex flex-row items-center justify-between">
          <CardTitle className="text-base font-medium">
            {t('students.detail.tabs.profile')}
          </CardTitle>
          {canManage && (
            <Button variant="outline" size="sm" onClick={() => setEditing(true)}>
              <Pencil />
              {t('students.detail.edit')}
            </Button>
          )}
        </CardHeader>
        <CardContent className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <ReadField label={t('profile.contact.firstNameLabel')} value={student.firstName} />
          <ReadField label={t('profile.contact.lastNameLabel')} value={student.lastName} />
          <ReadField label={t('common.middleName')} value={student.middleName || '—'} />
          <ReadField label={t('profile.studentData.dateOfBirth')} value={student.dateOfBirth} />
          <ReadField label={t('profile.contact.phoneLabel')} value={student.phoneNumber} />
          <ReadField label={t('profile.studentData.email')} value={student.email} />
          <ReadField label={t('profile.studentData.city')} value={student.city} />
          <ReadField label={t('profile.studentData.country')} value={student.country} />
          <ReadField
            label={t('profile.studentData.isChild')}
            value={student.isChild ? t('common.yes') : t('common.no')}
          />
        </CardContent>
      </Card>
    )
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-base font-medium">{t('students.detail.edit')}</CardTitle>
      </CardHeader>
      <CardContent>
        <form
          className="grid grid-cols-1 gap-4 sm:grid-cols-2"
          onSubmit={handleSubmit(onSubmit)}
          noValidate
        >
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
                  id="isChild-edit"
                />
              )}
            />
            <Label htmlFor="isChild-edit" className="font-normal">
              {t('profile.studentData.isChild')}
            </Label>
          </div>
          <div className="flex gap-2 sm:col-span-2">
            <Button type="submit" loading={isSubmitting}>
              {t('students.detail.save')}
            </Button>
            <Button
              type="button"
              variant="outline"
              onClick={() => {
                reset()
                setEditing(false)
              }}
            >
              {t('students.detail.cancelEdit')}
            </Button>
          </div>
        </form>
      </CardContent>
    </Card>
  )
}

function PreferencesTab({
  student,
  canManage,
  studentId,
}: {
  student: StudentDetailData
  canManage: boolean
  studentId: string
}) {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const [editing, setEditing] = useState(false)
  const prefs = student.preferences

  const schema = useMemo(
    () =>
      z.object({
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
    control,
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    values: {
      learningGoal: (prefs.learningGoal ?? 'Study') as FormValues['learningGoal'],
      format: (prefs.format ?? 'Online') as FormValues['format'],
      lessonType: prefs.lessonType,
      intensity: toNum(prefs.intensity),
      currentLevel: (prefs.currentLevel ?? 'A1') as FormValues['currentLevel'],
      hadPreviousCourses: prefs.hadPreviousCourses,
      languages: student.languages.filter((l): l is FormValues['languages'][number] => !!l),
    },
  })

  async function onSubmit(values: FormValues) {
    try {
      await updateStudentPreferences(studentId, values)
      await queryClient.invalidateQueries({ queryKey: ['students', studentId] })
      toast.success(t('students.detail.preferencesSaved'))
      setEditing(false)
    } catch (err) {
      toast.error(
        err instanceof ApiError ? err.detail || t('common.unknownError') : t('common.networkError'),
      )
    }
  }

  if (!editing) {
    return (
      <Card>
        <CardHeader className="flex flex-row items-center justify-between">
          <CardTitle className="text-base font-medium">
            {t('students.detail.tabs.preferences')}
          </CardTitle>
          {canManage && (
            <Button variant="outline" size="sm" onClick={() => setEditing(true)}>
              <Pencil />
              {t('students.detail.edit')}
            </Button>
          )}
        </CardHeader>
        <CardContent className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <ReadField
            label={t('profile.studentData.learningGoal')}
            value={enumLabel(t, 'learningGoal', prefs.learningGoal)}
          />
          <ReadField
            label={t('profile.studentData.format')}
            value={enumLabel(t, 'format', prefs.format)}
          />
          <ReadField
            label={t('profile.studentData.lessonType')}
            value={enumLabel(t, 'lessonType', prefs.lessonType)}
          />
          <ReadField label={t('profile.studentData.intensity')} value={String(prefs.intensity)} />
          <ReadField
            label={t('profile.studentData.currentLevel')}
            value={enumLabel(t, 'level', prefs.currentLevel)}
          />
          <ReadField
            label={t('profile.studentData.hadPreviousCourses')}
            value={prefs.hadPreviousCourses ? t('common.yes') : t('common.no')}
          />
          <div className="sm:col-span-2">
            <ReadField
              label={t('profile.studentData.languages')}
              value={student.languages.map((l) => enumLabel(t, 'language', l)).join(', ') || '—'}
            />
          </div>
        </CardContent>
      </Card>
    )
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-base font-medium">{t('students.detail.edit')}</CardTitle>
      </CardHeader>
      <CardContent>
        <form
          className="grid grid-cols-1 gap-4 sm:grid-cols-2"
          onSubmit={handleSubmit(onSubmit)}
          noValidate
        >
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
                  id="hadPrev-edit"
                />
              )}
            />
            <Label htmlFor="hadPrev-edit" className="font-normal">
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
          <div className="flex gap-2 sm:col-span-2">
            <Button type="submit" loading={isSubmitting}>
              {t('students.detail.save')}
            </Button>
            <Button
              type="button"
              variant="outline"
              onClick={() => {
                reset()
                setEditing(false)
              }}
            >
              {t('students.detail.cancelEdit')}
            </Button>
          </div>
        </form>
      </CardContent>
    </Card>
  )
}

function ParentTab({
  studentId,
  parentInfo,
  canManage,
}: {
  studentId: string
  parentInfo: StudentDetailData['parentInfo']
  canManage: boolean
}) {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const [editing, setEditing] = useState(false)
  const [removeOpen, setRemoveOpen] = useState(false)

  const schema = useMemo(
    () =>
      z.object({
        firstName: z.string().min(1),
        lastName: z.string().min(1),
        middleName: z.string().optional(),
        phoneNumber: z.string().min(1),
        email: z.string().email(),
      }),
    [],
  )
  type FormValues = z.infer<typeof schema>

  const {
    register,
    handleSubmit,
    reset,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    values: {
      firstName: parentInfo?.firstName ?? '',
      lastName: parentInfo?.lastName ?? '',
      middleName: parentInfo?.middleName ?? '',
      phoneNumber: parentInfo?.phoneNumber ?? '',
      email: parentInfo?.email ?? '',
    },
  })

  const deleteMutation = useMutation({
    mutationFn: () => deleteParentInfo(studentId),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['students', studentId] })
      toast.success(t('students.detail.removeParent'))
    },
  })

  async function onSubmit(values: FormValues) {
    try {
      const input = { ...values, middleName: values.middleName || null }
      if (parentInfo) {
        await updateParentInfo(studentId, input)
      } else {
        await addParentInfo(studentId, input)
      }
      await queryClient.invalidateQueries({ queryKey: ['students', studentId] })
      toast.success(t('students.detail.profileSaved'))
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

  if (!editing) {
    return (
      <Card>
        <CardHeader className="flex flex-row items-center justify-between">
          <CardTitle className="text-base font-medium">
            {t('students.detail.tabs.parent')}
          </CardTitle>
          {canManage && parentInfo && (
            <div className="flex gap-2">
              <Button variant="outline" size="sm" onClick={() => setEditing(true)}>
                <Pencil />
                {t('students.detail.editParent')}
              </Button>
              <Button variant="outline" size="sm" onClick={() => setRemoveOpen(true)}>
                <Trash2 />
                {t('students.detail.removeParent')}
              </Button>
            </div>
          )}
        </CardHeader>
        <CardContent className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          {parentInfo ? (
            <>
              <ReadField label={t('profile.contact.firstNameLabel')} value={parentInfo.firstName} />
              <ReadField label={t('profile.contact.lastNameLabel')} value={parentInfo.lastName} />
              <ReadField label={t('common.middleName')} value={parentInfo.middleName || '—'} />
              <ReadField label={t('profile.contact.phoneLabel')} value={parentInfo.phoneNumber} />
              <ReadField label={t('profile.studentData.email')} value={parentInfo.email} />
            </>
          ) : (
            <div className="flex flex-col items-start gap-3 sm:col-span-2">
              <p className="text-sm text-muted-foreground">{t('students.detail.noParent')}</p>
              {canManage && (
                <Button variant="outline" size="sm" onClick={() => setEditing(true)}>
                  <Plus />
                  {t('students.detail.addParent')}
                </Button>
              )}
            </div>
          )}
        </CardContent>

        <ConfirmDialog
          open={removeOpen}
          onOpenChange={setRemoveOpen}
          title={t('students.detail.confirmRemoveParentTitle')}
          destructive
          onConfirm={() => deleteMutation.mutateAsync()}
        />
      </Card>
    )
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-base font-medium">
          {parentInfo ? t('students.detail.editParent') : t('students.detail.addParent')}
        </CardTitle>
      </CardHeader>
      <CardContent>
        <form
          className="grid grid-cols-1 gap-4 sm:grid-cols-2"
          onSubmit={handleSubmit(onSubmit)}
          noValidate
        >
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
          <Field label={t('profile.contact.phoneLabel')} error={errors.phoneNumber?.message}>
            <Input type="tel" {...register('phoneNumber')} aria-invalid={!!errors.phoneNumber} />
          </Field>
          <Field label={t('profile.studentData.email')} error={errors.email?.message}>
            <Input type="email" {...register('email')} aria-invalid={!!errors.email} />
          </Field>
          <div className="flex gap-2 sm:col-span-2">
            <Button type="submit" loading={isSubmitting}>
              {t('students.detail.save')}
            </Button>
            <Button
              type="button"
              variant="outline"
              onClick={() => {
                reset()
                setEditing(false)
              }}
            >
              {t('students.detail.cancelEdit')}
            </Button>
          </div>
        </form>
      </CardContent>
    </Card>
  )
}

function CommentTab({
  studentId,
  comment,
  canManage,
}: {
  studentId: string
  comment: string | null
  canManage: boolean
}) {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const [value, setValue] = useState(comment ?? '')
  const [saving, setSaving] = useState(false)

  async function handleSave() {
    setSaving(true)
    try {
      await updateStudentComment(studentId, value || null)
      await queryClient.invalidateQueries({ queryKey: ['students', studentId] })
      toast.success(t('students.detail.commentSaved'))
    } catch (err) {
      toast.error(
        err instanceof ApiError ? err.detail || t('common.unknownError') : t('common.networkError'),
      )
    } finally {
      setSaving(false)
    }
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-base font-medium">{t('students.detail.commentLabel')}</CardTitle>
      </CardHeader>
      <CardContent className="flex flex-col gap-3">
        <Textarea
          value={value}
          onChange={(e) => setValue(e.target.value)}
          placeholder={t('students.detail.commentPlaceholder')}
          disabled={!canManage}
          rows={6}
        />
        {canManage && (
          <Button className="self-start" loading={saving} onClick={handleSave}>
            {t('students.detail.save')}
          </Button>
        )}
      </CardContent>
    </Card>
  )
}

function EnrollmentsTab({ studentId, studentName }: { studentId: string; studentName: string }) {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const canManageEnrollments = useCan('CanManageEnrollments')
  const [enrollOpen, setEnrollOpen] = useState(false)

  const { data: enrollments } = useQuery({
    queryKey: ['enrollments', { studentId }],
    queryFn: () => getEnrollments({ studentId, pageSize: 50 }),
  })

  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between">
        <CardTitle className="text-base font-medium">
          {t('students.detail.tabs.enrollments')}
        </CardTitle>
        {canManageEnrollments && (
          <Button variant="outline" size="sm" onClick={() => setEnrollOpen(true)}>
            <Plus />
            {t('enrollments.createDialog.title')}
          </Button>
        )}
      </CardHeader>
      <CardContent>
        {enrollments && enrollments.items.length > 0 ? (
          <EnrollmentsTable rows={enrollments.items} hideColumn="student" />
        ) : (
          <p className="text-sm text-muted-foreground">{t('students.detail.noEnrollments')}</p>
        )}
      </CardContent>

      <CreateEnrollmentDialog
        open={enrollOpen}
        onOpenChange={setEnrollOpen}
        fixedStudentId={studentId}
        fixedStudentLabel={studentName}
        onCreated={() =>
          queryClient.invalidateQueries({ queryKey: ['enrollments', { studentId }] })
        }
      />
    </Card>
  )
}

const LESSONS_PAGE_SIZE = 10

function LessonsTab({ studentId }: { studentId: string }) {
  const { t, i18n } = useTranslation()
  const lang = i18n.language === 'en' ? 'en' : 'uk'
  const { rows, isLoading } = useStudentLessons(studentId)
  const [page, setPage] = useState(1)

  // useStudentLessons assembles the full history client-side (see its own
  // comment - there's no `GET /api/schedules?studentId=` filter to page
  // through on the backend), so pagination here slices the already-sorted
  // in-memory array instead of a page/pageSize query param.
  const totalPages = Math.max(1, Math.ceil(rows.length / LESSONS_PAGE_SIZE))
  const currentPage = Math.min(page, totalPages)
  const pageRows = rows.slice(
    (currentPage - 1) * LESSONS_PAGE_SIZE,
    currentPage * LESSONS_PAGE_SIZE,
  )

  const columns: DataTableColumn<(typeof rows)[number]>[] = [
    {
      key: 'date',
      header: t('calendar.item.title'),
      cell: (r) => formatDateTime(r.row.scheduledDate, lang),
    },
    {
      key: 'course',
      header: t('students.detail.tabs.lessonsColumns.course'),
      cell: (r) => r.courseName ?? '—',
    },
    {
      key: 'teacher',
      header: t('calendar.item.teacher'),
      cell: (r) =>
        r.groupName ? `${r.teacherName ?? '—'} · ${r.groupName}` : (r.teacherName ?? '—'),
    },
    {
      key: 'status',
      header: t('students.detail.tabs.lessonsColumns.status'),
      cell: (r) => (
        <Badge
          variant={
            r.row.status === 'Cancelled'
              ? 'destructive'
              : r.row.status === 'Completed'
                ? 'success'
                : 'secondary'
          }
        >
          {enumLabel(t, 'scheduleStatus', r.row.status)}
        </Badge>
      ),
    },
  ]

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-base font-medium">{t('students.detail.tabs.lessons')}</CardTitle>
      </CardHeader>
      <CardContent className="flex flex-col gap-4">
        <DataTable
          columns={columns}
          rows={pageRows}
          rowKey={(r) => r.row.id}
          isLoading={isLoading}
          emptyMessage={t('students.detail.noLessons')}
        />
        {rows.length > 0 && (
          <Pagination
            page={currentPage}
            totalPages={totalPages}
            totalCount={rows.length}
            onPageChange={setPage}
          />
        )}
      </CardContent>
    </Card>
  )
}

function InvoicesTab({ studentId }: { studentId: string }) {
  const { t, i18n } = useTranslation()
  const lang = i18n.language === 'en' ? 'en' : 'uk'
  const [page, setPage] = useState(1)

  const { data, isPending } = useQuery({
    queryKey: ['invoices', { studentId, page }],
    queryFn: () => getInvoices({ studentId, page, pageSize: 10 }),
  })
  const resolved = useResolvedInvoices(data?.items ?? [])

  const columns: DataTableColumn<(typeof resolved)[number]>[] = [
    {
      key: 'period',
      header: t('billing.invoices.columns.period'),
      cell: (r) => r.row.period,
    },
    {
      key: 'amount',
      header: t('billing.invoices.columns.amount'),
      cell: (r) => formatCurrency(r.row.amount, lang),
    },
    {
      key: 'dueDate',
      header: t('billing.invoices.columns.dueDate'),
      cell: (r) => formatDate(r.row.dueDate, lang),
    },
    {
      key: 'status',
      header: t('billing.invoices.columns.status'),
      cell: (r) => (
        <Badge
          variant={
            r.row.status === 'Paid'
              ? 'success'
              : r.row.status === 'Overdue'
                ? 'destructive'
                : 'warning'
          }
        >
          {enumLabel(t, 'invoiceStatus', r.row.status)}
        </Badge>
      ),
    },
  ]

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-base font-medium">
          {t('students.detail.tabs.invoices')}
        </CardTitle>
      </CardHeader>
      <CardContent className="flex flex-col gap-4">
        <DataTable
          columns={columns}
          rows={resolved}
          rowKey={(r) => r.row.id}
          isLoading={isPending}
          emptyMessage={t('billing.invoices.empty')}
        />
        {data && (
          <Pagination
            page={toNum(data.page)}
            totalPages={toNum(data.totalPages)}
            totalCount={toNum(data.totalCount)}
            onPageChange={setPage}
          />
        )}
      </CardContent>
    </Card>
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
