import { useMemo, useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { ArrowLeft, Pencil, UserX, UserCheck, Trash2, Plus } from 'lucide-react'
import {
  getTeacherById,
  updateTeacher,
  updateTeacherComment,
  changeTeacherStatus,
  deleteTeacher,
  addSalaryRate,
} from '@/features/teachers/api'
import { ApiError } from '@/api/errors'
import { applyServerValidation } from '@/lib/applyServerValidation'
import { useAuthenticatedBlobUrl } from '@/lib/useAuthenticatedBlobUrl'
import { enumLabel } from '@/lib/enumLabels'
import { toNum, formatCurrency, formatDate } from '@/lib/utils'
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
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar'
import { Badge } from '@/components/ui/badge'
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
import { CreateProfileAccountDialog } from '@/components/shared/CreateProfileAccountDialog'
import { TemporaryPasswordDialog } from '@/components/shared/TemporaryPasswordDialog'
import { Field } from '@/components/shared/Field'

const ACTIVE_STATUSES = ['Probation', 'Employed'] as const
const INACTIVE_STATUSES = ['OnLeave', 'Resigned', 'Dismissed'] as const

function statusVariant(status: string): 'success' | 'warning' | 'secondary' | 'destructive' {
  if (status === 'Employed' || status === 'Probation') return 'success'
  if (status === 'OnLeave') return 'warning'
  if (status === 'Resigned') return 'secondary'
  return 'destructive'
}

export function TeacherDetailPage() {
  const { id = '' } = useParams()
  const { t } = useTranslation()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const canManage = useCan('CanManageTeachers')
  const canDelete = useCan('CanDeleteTeachers')
  const canCreateAccount = useCan('CanManageAdmins')

  const [activeTab, setActiveTab] = useState('profile')
  const [deactivateOpen, setDeactivateOpen] = useState(false)
  const [reactivateOpen, setReactivateOpen] = useState(false)
  const [deleteOpen, setDeleteOpen] = useState(false)
  const [deactivateTarget, setDeactivateTarget] =
    useState<(typeof INACTIVE_STATUSES)[number]>('OnLeave')
  const [createAccountOpen, setCreateAccountOpen] = useState(false)
  const [createdAccount, setCreatedAccount] = useState<{
    email: string
    temporaryPassword: string
  } | null>(null)

  const { data: teacher, isLoading } = useQuery({
    queryKey: ['teachers', id],
    queryFn: () => getTeacherById(id),
    enabled: !!id,
  })

  const photoUrl = useAuthenticatedBlobUrl(teacher ? `/api/teachers/${id}/photo` : null)

  const statusMutation = useMutation({
    mutationFn: (status: (typeof ACTIVE_STATUSES)[number] | (typeof INACTIVE_STATUSES)[number]) =>
      changeTeacherStatus(id, status),
    onSuccess: async (result) => {
      await queryClient.invalidateQueries({ queryKey: ['teachers', id] })
      await queryClient.invalidateQueries({ queryKey: ['teachers'] })
      const lines = [
        toNum(result.cancelledLessons)
          ? t('teachers.detail.cancelledLessons', { count: toNum(result.cancelledLessons) })
          : null,
        toNum(result.restoredLessons)
          ? t('teachers.detail.restoredLessons', { count: toNum(result.restoredLessons) })
          : null,
        toNum(result.skippedLessons)
          ? t('teachers.detail.skippedLessons', { count: toNum(result.skippedLessons) })
          : null,
        result.hint,
      ].filter(Boolean)
      toast.success(t('teachers.detail.statusChangeResultTitle'), {
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
    mutationFn: () => deleteTeacher(id),
    onSuccess: () => {
      toast.success(t('teachers.detail.delete'))
      navigate('/teachers')
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

  if (isLoading || !teacher) {
    return (
      <div className="flex max-w-3xl flex-col gap-4">
        <Skeleton className="h-8 w-64" />
        <Skeleton className="h-40 w-full" />
      </div>
    )
  }

  const fullName = [teacher.lastName, teacher.firstName, teacher.middleName]
    .filter(Boolean)
    .join(' ')
  const initials = `${teacher.firstName[0] ?? ''}${teacher.lastName[0] ?? ''}`.toUpperCase()
  const isActive = (ACTIVE_STATUSES as readonly string[]).includes(teacher.status)

  return (
    <div className="flex max-w-3xl flex-col gap-6">
      <div className="flex flex-col gap-4">
        <Link
          to="/teachers"
          className="flex w-fit items-center gap-1 text-sm text-muted-foreground hover:text-foreground"
        >
          <ArrowLeft className="size-4" />
          {t('teachers.detail.backToList')}
        </Link>

        <div className="flex flex-wrap items-center gap-4">
          <Avatar className="size-16">
            {photoUrl && <AvatarImage src={photoUrl} alt="" />}
            <AvatarFallback className="text-lg">{initials || '?'}</AvatarFallback>
          </Avatar>
          <div className="flex flex-col gap-1">
            <h1 className="text-2xl font-semibold tracking-tight">{fullName}</h1>
            <div className="flex flex-wrap items-center gap-2">
              <Badge variant={statusVariant(teacher.status)}>
                {enumLabel(t, 'teacherStatus', teacher.status)}
              </Badge>
              {teacher.hasAccount && <Badge variant="secondary">{t('table.hasAccount')}</Badge>}
            </div>
          </div>

          <div className="ml-auto flex flex-wrap gap-2">
            {canCreateAccount && !teacher.hasAccount && (
              <Button variant="outline" onClick={() => setCreateAccountOpen(true)}>
                <UserCheck />
                {t('accountLink.createButton')}
              </Button>
            )}
            {canManage && (
              <>
                {isActive ? (
                  <Button variant="outline" onClick={() => setDeactivateOpen(true)}>
                    <UserX />
                    {t('teachers.detail.deactivate')}
                  </Button>
                ) : (
                  <Button variant="outline" onClick={() => setReactivateOpen(true)}>
                    <UserCheck />
                    {t('teachers.detail.reactivate')}
                  </Button>
                )}
                {canDelete && (
                  <Button variant="outline" onClick={() => setDeleteOpen(true)}>
                    <Trash2 />
                    {t('teachers.detail.delete')}
                  </Button>
                )}
              </>
            )}
          </div>
        </div>
      </div>

      {deleteConflict && (
        <div className="rounded-md border border-destructive/30 bg-destructive/10 p-3 text-sm text-destructive">
          <strong>{t('teachers.detail.deleteConflictTitle')}</strong> —{' '}
          {t('teachers.detail.deleteConflictDesc')}
        </div>
      )}

      <Tabs value={activeTab} onValueChange={setActiveTab}>
        <TabsList>
          <TabsTrigger value="profile">{t('teachers.detail.tabs.profile')}</TabsTrigger>
          <TabsTrigger value="salary">{t('teachers.detail.tabs.salary')}</TabsTrigger>
          <TabsTrigger value="comment">{t('teachers.detail.tabs.comment')}</TabsTrigger>
          <TabsTrigger value="lessons">{t('teachers.detail.tabs.lessons')}</TabsTrigger>
          <TabsTrigger value="payroll">{t('teachers.detail.tabs.payroll')}</TabsTrigger>
        </TabsList>

        <TabsContent value="profile">
          <ProfileTab teacher={teacher} canManage={canManage} teacherId={id} />
        </TabsContent>
        <TabsContent value="salary">
          <SalaryTab teacher={teacher} canManage={canManage} teacherId={id} />
        </TabsContent>
        <TabsContent value="comment">
          <CommentTab teacherId={id} comment={teacher.comment} canManage={canManage} />
        </TabsContent>
        <TabsContent value="lessons">
          <PlaceholderTab titleKey="teachers.detail.tabs.lessons" />
        </TabsContent>
        <TabsContent value="payroll">
          <PlaceholderTab titleKey="teachers.detail.tabs.payroll" />
        </TabsContent>
      </Tabs>

      <Dialog open={deactivateOpen} onOpenChange={setDeactivateOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t('teachers.detail.confirmDeactivateTitle')}</DialogTitle>
            <DialogDescription>{t('teachers.detail.confirmDeactivateDesc')}</DialogDescription>
          </DialogHeader>
          <Field label={t('teachers.detail.newStatusLabel')}>
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
                    {enumLabel(t, 'teacherStatus', v)}
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
        title={t('teachers.detail.confirmReactivateTitle')}
        description={t('teachers.detail.confirmReactivateDesc')}
        onConfirm={async () => {
          await statusMutation.mutateAsync('Employed')
        }}
      />
      <ConfirmDialog
        open={deleteOpen}
        onOpenChange={setDeleteOpen}
        title={t('teachers.detail.confirmDeleteTitle')}
        description={t('teachers.detail.confirmDeleteDesc')}
        destructive
        onConfirm={handleDelete}
      />

      <CreateProfileAccountDialog
        open={createAccountOpen}
        onOpenChange={setCreateAccountOpen}
        profileType="Teacher"
        profileId={id}
        defaultEmail={teacher.email}
        onCreated={(result) => {
          queryClient.invalidateQueries({ queryKey: ['teachers', id] })
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

type TeacherDetailData = NonNullable<
  ReturnType<typeof useQuery<Awaited<ReturnType<typeof getTeacherById>>>>['data']
>

function ProfileTab({
  teacher,
  canManage,
  teacherId,
}: {
  teacher: TeacherDetailData
  canManage: boolean
  teacherId: string
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
      }),
    [],
  )
  type FormValues = z.infer<typeof schema>

  const {
    register,
    handleSubmit,
    setError,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    values: {
      firstName: teacher.firstName,
      lastName: teacher.lastName,
      middleName: teacher.middleName ?? '',
      dateOfBirth: teacher.dateOfBirth,
      phoneNumber: teacher.phoneNumber,
      city: teacher.city,
      country: teacher.country,
    },
  })

  async function onSubmit(values: FormValues) {
    try {
      await updateTeacher(teacherId, { ...values, middleName: values.middleName || null })
      await queryClient.invalidateQueries({ queryKey: ['teachers', teacherId] })
      await queryClient.invalidateQueries({ queryKey: ['teachers'] })
      toast.success(t('teachers.detail.profileSaved'))
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
            {t('teachers.detail.tabs.profile')}
          </CardTitle>
          {canManage && (
            <Button variant="outline" size="sm" onClick={() => setEditing(true)}>
              <Pencil />
              {t('teachers.detail.edit')}
            </Button>
          )}
        </CardHeader>
        <CardContent className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <ReadField label={t('profile.contact.firstNameLabel')} value={teacher.firstName} />
          <ReadField label={t('profile.contact.lastNameLabel')} value={teacher.lastName} />
          <ReadField label={t('common.middleName')} value={teacher.middleName || '—'} />
          <ReadField label={t('profile.teacherData.dateOfBirth')} value={teacher.dateOfBirth} />
          <ReadField label={t('profile.contact.phoneLabel')} value={teacher.phoneNumber} />
          <ReadField label={t('profile.teacherData.email')} value={teacher.email} />
          <ReadField label={t('profile.teacherData.city')} value={teacher.city} />
          <ReadField label={t('profile.teacherData.country')} value={teacher.country} />
        </CardContent>
      </Card>
    )
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-base font-medium">{t('teachers.detail.edit')}</CardTitle>
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
          <Field label={t('profile.teacherData.dateOfBirth')} error={errors.dateOfBirth?.message}>
            <Input type="date" {...register('dateOfBirth')} aria-invalid={!!errors.dateOfBirth} />
          </Field>
          <Field label={t('profile.contact.phoneLabel')} error={errors.phoneNumber?.message}>
            <Input type="tel" {...register('phoneNumber')} aria-invalid={!!errors.phoneNumber} />
          </Field>
          <Field label={t('profile.teacherData.city')} error={errors.city?.message}>
            <Input {...register('city')} aria-invalid={!!errors.city} />
          </Field>
          <Field label={t('profile.teacherData.country')} error={errors.country?.message}>
            <Input {...register('country')} aria-invalid={!!errors.country} />
          </Field>
          <div className="flex gap-2 sm:col-span-2">
            <Button type="submit" loading={isSubmitting}>
              {t('teachers.detail.save')}
            </Button>
            <Button
              type="button"
              variant="outline"
              onClick={() => {
                reset()
                setEditing(false)
              }}
            >
              {t('teachers.detail.cancelEdit')}
            </Button>
          </div>
        </form>
      </CardContent>
    </Card>
  )
}

function SalaryTab({
  teacher,
  canManage,
  teacherId,
}: {
  teacher: TeacherDetailData
  canManage: boolean
  teacherId: string
}) {
  const { t, i18n } = useTranslation()
  const lang = i18n.language === 'en' ? 'en' : 'uk'
  const queryClient = useQueryClient()
  const [addOpen, setAddOpen] = useState(false)

  const schema = useMemo(
    () =>
      z.object({
        baseSalary: z.coerce.number().min(0),
        lessonsRate: z.coerce.number().min(0),
        effectiveFrom: z.string().min(1),
      }),
    [],
  )
  type FormValues = z.infer<typeof schema>

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      baseSalary: 0,
      lessonsRate: 0,
      effectiveFrom: new Date().toISOString().slice(0, 10),
    },
  })

  async function onSubmit(values: FormValues) {
    try {
      await addSalaryRate(teacherId, {
        baseSalary: values.baseSalary,
        lessonsRate: values.lessonsRate,
        effectiveFrom: new Date(values.effectiveFrom).toISOString(),
      })
      await queryClient.invalidateQueries({ queryKey: ['teachers', teacherId] })
      toast.success(t('teachers.detail.rateSaved'))
      setAddOpen(false)
      reset()
    } catch (err) {
      toast.error(
        err instanceof ApiError ? err.detail || t('common.unknownError') : t('common.networkError'),
      )
    }
  }

  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between">
        <CardTitle className="text-base font-medium">
          {t('profile.teacherData.salaryTitle')}
        </CardTitle>
        {canManage && (
          <Button variant="outline" size="sm" onClick={() => setAddOpen(true)}>
            <Plus />
            {t('teachers.detail.addRate')}
          </Button>
        )}
      </CardHeader>
      <CardContent className="flex flex-col gap-4">
        {teacher.currentSalaryRate && (
          <div className="rounded-md border border-border p-3">
            <p className="mb-2 text-xs font-medium text-muted-foreground">
              {t('profile.teacherData.currentRate')}
            </p>
            <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
              <ReadField
                label={t('profile.teacherData.baseSalary')}
                value={formatCurrency(teacher.currentSalaryRate.baseSalary, lang)}
              />
              <ReadField
                label={t('profile.teacherData.lessonsRate')}
                value={formatCurrency(teacher.currentSalaryRate.lessonsRate, lang)}
              />
              <ReadField
                label={t('profile.teacherData.effectiveFrom')}
                value={formatDate(teacher.currentSalaryRate.effectiveFrom, lang)}
              />
            </div>
          </div>
        )}

        {teacher.salaryRates.length > 0 ? (
          <div className="flex flex-col gap-2">
            {teacher.salaryRates.map((rate) => (
              <div
                key={rate.id}
                className="flex flex-wrap items-center gap-4 rounded-md border border-border p-3 text-sm"
              >
                <span>{formatCurrency(rate.baseSalary, lang)}</span>
                <span className="text-muted-foreground">
                  + {formatCurrency(rate.lessonsRate, lang)}/
                  {t('teachers.detail.tabs.lessons').toLowerCase()}
                </span>
                <span className="ml-auto text-muted-foreground">
                  {formatDate(rate.effectiveFrom, lang)}
                </span>
              </div>
            ))}
          </div>
        ) : (
          <p className="text-sm text-muted-foreground">{t('profile.teacherData.noRates')}</p>
        )}
      </CardContent>

      <Dialog open={addOpen} onOpenChange={setAddOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t('teachers.detail.addRateTitle')}</DialogTitle>
          </DialogHeader>
          <form className="flex flex-col gap-4" onSubmit={handleSubmit(onSubmit)} noValidate>
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
            <Field
              label={t('profile.teacherData.effectiveFrom')}
              error={errors.effectiveFrom?.message}
            >
              <Input
                type="date"
                {...register('effectiveFrom')}
                aria-invalid={!!errors.effectiveFrom}
              />
            </Field>
            <DialogFooter>
              <Button
                type="button"
                variant="outline"
                onClick={() => setAddOpen(false)}
                disabled={isSubmitting}
              >
                {t('common.cancel')}
              </Button>
              <Button type="submit" loading={isSubmitting}>
                {t('common.save')}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </Card>
  )
}

function CommentTab({
  teacherId,
  comment,
  canManage,
}: {
  teacherId: string
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
      await updateTeacherComment(teacherId, value || null)
      await queryClient.invalidateQueries({ queryKey: ['teachers', teacherId] })
      toast.success(t('teachers.detail.commentSaved'))
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
        <CardTitle className="text-base font-medium">{t('teachers.detail.commentLabel')}</CardTitle>
      </CardHeader>
      <CardContent className="flex flex-col gap-3">
        <Textarea
          value={value}
          onChange={(e) => setValue(e.target.value)}
          placeholder={t('teachers.detail.commentPlaceholder')}
          disabled={!canManage}
          rows={6}
        />
        {canManage && (
          <Button className="self-start" loading={saving} onClick={handleSave}>
            {t('teachers.detail.save')}
          </Button>
        )}
      </CardContent>
    </Card>
  )
}

function PlaceholderTab({ titleKey }: { titleKey: string }) {
  const { t } = useTranslation()
  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-base font-medium">{t(titleKey)}</CardTitle>
      </CardHeader>
      <CardContent>
        <p className="text-sm text-muted-foreground">{t('common.comingSoon')}</p>
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
