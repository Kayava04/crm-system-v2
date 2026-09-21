import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { ArrowLeft, Tag, PercentCircle } from 'lucide-react'
import {
  getEnrollmentById,
  activateEnrollment,
  suspendEnrollment,
  completeEnrollment,
  terminateEnrollment,
  setEnrollmentPrice,
  applyEnrollmentDiscount,
  removeEnrollmentDiscount,
  updateEnrollmentComment,
  updateEnrollmentPreferredSchedule,
} from '@/features/enrollments/api'
import { getStudentById } from '@/features/students/api'
import { getCourseById } from '@/features/courses/api'
import { ApiError } from '@/api/errors'
import { enumLabel } from '@/lib/enumLabels'
import { toNum, formatCurrency, formatDate } from '@/lib/utils'
import { useCan } from '@/features/auth/useCan'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Skeleton } from '@/components/ui/skeleton'
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { ConfirmDialog } from '@/components/shared/ConfirmDialog'
import { Field } from '@/components/shared/Field'

function statusVariant(status: string): 'success' | 'warning' | 'secondary' | 'destructive' {
  if (status === 'Active') return 'success'
  if (status === 'Suspended') return 'warning'
  if (status === 'Completed') return 'secondary'
  return 'destructive'
}

export function EnrollmentDetailPage() {
  const { id = '' } = useParams()
  const { t, i18n } = useTranslation()
  const lang = i18n.language === 'en' ? 'en' : 'uk'
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const canManage = useCan('CanManageEnrollments')

  const [activateOpen, setActivateOpen] = useState(false)
  const [suspendOpen, setSuspendOpen] = useState(false)
  const [completeOpen, setCompleteOpen] = useState(false)
  const [terminateOpen, setTerminateOpen] = useState(false)
  const [priceOpen, setPriceOpen] = useState(false)
  const [discountOpen, setDiscountOpen] = useState(false)

  const { data: enrollment, isLoading } = useQuery({
    queryKey: ['enrollments', id],
    queryFn: () => getEnrollmentById(id),
    enabled: !!id,
  })

  const { data: student, isError: studentError } = useQuery({
    queryKey: ['students', enrollment?.studentId, 'lookup-name'],
    queryFn: () => getStudentById(enrollment!.studentId),
    enabled: !!enrollment?.studentId,
    retry: false,
  })
  const { data: course, isError: courseError } = useQuery({
    queryKey: ['courses', enrollment?.courseId],
    queryFn: () => getCourseById(enrollment!.courseId),
    enabled: !!enrollment?.courseId,
    retry: false,
  })

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ['enrollments', id] })
    queryClient.invalidateQueries({ queryKey: ['enrollments'] })
  }

  function showLifecycleResult(result: {
    cancelledLessons: number | string
    restoredLessons: number | string
    skippedLessons: number | string
    lessonsLeftToSchedule: number | string
  }) {
    const lines = [
      toNum(result.cancelledLessons)
        ? t('enrollments.detail.cancelledLessons', { count: toNum(result.cancelledLessons) })
        : null,
      toNum(result.restoredLessons)
        ? t('enrollments.detail.restoredLessons', { count: toNum(result.restoredLessons) })
        : null,
      toNum(result.skippedLessons)
        ? t('enrollments.detail.skippedLessons', { count: toNum(result.skippedLessons) })
        : null,
      toNum(result.lessonsLeftToSchedule)
        ? t('enrollments.detail.lessonsLeftToSchedule', {
            count: toNum(result.lessonsLeftToSchedule),
          })
        : null,
    ].filter(Boolean)
    toast.success(t('enrollments.detail.statusChangeResultTitle'), {
      description: lines.length ? lines.join(' · ') : undefined,
    })
  }

  function onMutationError(err: unknown) {
    toast.error(
      err instanceof ApiError ? err.detail || t('common.unknownError') : t('common.networkError'),
    )
  }

  const activateMutation = useMutation({
    mutationFn: () => activateEnrollment(id),
    onSuccess: (r) => {
      invalidate()
      showLifecycleResult(r)
    },
    onError: onMutationError,
  })
  const suspendMutation = useMutation({
    mutationFn: () => suspendEnrollment(id),
    onSuccess: (r) => {
      invalidate()
      showLifecycleResult(r)
    },
    onError: onMutationError,
  })
  const completeMutation = useMutation({
    mutationFn: () => completeEnrollment(id),
    onSuccess: (r) => {
      invalidate()
      showLifecycleResult(r)
    },
    onError: onMutationError,
  })
  const terminateMutation = useMutation({
    mutationFn: () => terminateEnrollment(id),
    onSuccess: (r) => {
      invalidate()
      showLifecycleResult(r)
    },
    onError: onMutationError,
  })

  if (isLoading || !enrollment) {
    return (
      <div className="mx-auto flex w-full max-w-2xl flex-col gap-4">
        <Skeleton className="h-8 w-64" />
        <Skeleton className="h-40 w-full" />
      </div>
    )
  }

  const canActivate = enrollment.status === 'Draft' || enrollment.status === 'Suspended'
  const canSuspendOrComplete = enrollment.status === 'Active'
  const canTerminate =
    enrollment.status === 'Active' ||
    enrollment.status === 'Suspended' ||
    enrollment.status === 'Draft'

  return (
    <div className="mx-auto flex w-full max-w-2xl flex-col gap-6">
      <div className="flex flex-col gap-4">
        <button
          type="button"
          onClick={() => navigate(-1)}
          className="flex w-fit items-center gap-1 text-sm text-muted-foreground hover:text-foreground"
        >
          <ArrowLeft className="size-4" />
          {t('common.back')}
        </button>

        <div className="flex flex-wrap items-center gap-3">
          <h1 className="text-2xl font-semibold tracking-tight">
            {t('enrollments.detail.title', { number: enrollment.enrollmentNumber })}
          </h1>
          <Badge variant={statusVariant(enrollment.status)}>
            {enumLabel(t, 'enrollmentStatus', enrollment.status)}
          </Badge>
        </div>

        {canManage && (
          <div className="flex flex-wrap gap-2">
            {canActivate && (
              <Button variant="outline" onClick={() => setActivateOpen(true)}>
                {t('enrollments.detail.activate')}
              </Button>
            )}
            {canSuspendOrComplete && (
              <>
                <Button variant="outline" onClick={() => setSuspendOpen(true)}>
                  {t('enrollments.detail.suspend')}
                </Button>
                <Button variant="outline" onClick={() => setCompleteOpen(true)}>
                  {t('enrollments.detail.complete')}
                </Button>
              </>
            )}
            {canTerminate && (
              <Button variant="outline" onClick={() => setTerminateOpen(true)}>
                {t('enrollments.detail.terminate')}
              </Button>
            )}
            <Button variant="outline" onClick={() => setPriceOpen(true)}>
              <Tag />
              {t('enrollments.detail.setPriceTitle')}
            </Button>
            <Button variant="outline" onClick={() => setDiscountOpen(true)}>
              <PercentCircle />
              {t('enrollments.detail.discountTitle')}
            </Button>
          </div>
        )}
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="text-base font-medium">
            {t('enrollments.detail.student')} / {t('enrollments.detail.course')}
          </CardTitle>
        </CardHeader>
        <CardContent className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <div className="flex flex-col gap-1">
            <span className="text-xs text-muted-foreground">{t('enrollments.detail.student')}</span>
            {student ? (
              <Link
                to={`/students/${student.id}`}
                className="text-sm font-medium text-primary hover:underline"
              >
                {student.lastName} {student.firstName}
              </Link>
            ) : studentError ? (
              <span className="text-sm text-muted-foreground">{enrollment.studentId}</span>
            ) : (
              <Skeleton className="h-4 w-32" />
            )}
          </div>
          <div className="flex flex-col gap-1">
            <span className="text-xs text-muted-foreground">{t('enrollments.detail.course')}</span>
            {course ? (
              <Link
                to={`/courses/${course.id}`}
                className="text-sm font-medium text-primary hover:underline"
              >
                {course.name}
              </Link>
            ) : courseError ? (
              <span className="text-sm text-muted-foreground">{enrollment.courseId}</span>
            ) : (
              <Skeleton className="h-4 w-32" />
            )}
          </div>
          <ReadField
            label={t('enrollments.detail.startDate')}
            value={formatDate(enrollment.startDate, lang)}
          />
          <ReadField
            label={t('enrollments.detail.endDate')}
            value={formatDate(enrollment.endDate, lang)}
          />
          <ReadField
            label={t('enrollments.detail.coursePrice')}
            value={formatCurrency(enrollment.coursePrice, lang)}
          />
          <ReadField
            label={t('enrollments.detail.discountedPrice')}
            value={
              enrollment.discountedPrice != null
                ? formatCurrency(enrollment.discountedPrice, lang)
                : '—'
            }
          />
          <ReadField
            label={t('enrollments.detail.effectivePrice')}
            value={formatCurrency(enrollment.effectivePrice, lang)}
          />
        </CardContent>
      </Card>

      <PreferredScheduleCard
        enrollmentId={id}
        value={enrollment.preferredSchedule}
        canManage={canManage}
      />
      <CommentCard enrollmentId={id} value={enrollment.comment} canManage={canManage} />

      <ConfirmDialog
        open={activateOpen}
        onOpenChange={setActivateOpen}
        title={t('enrollments.detail.confirmActivateTitle')}
        description={t('enrollments.detail.confirmActivateDesc')}
        onConfirm={async () => {
          await activateMutation.mutateAsync()
        }}
      />
      <ConfirmDialog
        open={suspendOpen}
        onOpenChange={setSuspendOpen}
        title={t('enrollments.detail.confirmSuspendTitle')}
        description={t('enrollments.detail.confirmSuspendDesc')}
        destructive
        onConfirm={async () => {
          await suspendMutation.mutateAsync()
        }}
      />
      <ConfirmDialog
        open={completeOpen}
        onOpenChange={setCompleteOpen}
        title={t('enrollments.detail.confirmCompleteTitle')}
        description={t('enrollments.detail.confirmCompleteDesc')}
        onConfirm={async () => {
          await completeMutation.mutateAsync()
        }}
      />
      <ConfirmDialog
        open={terminateOpen}
        onOpenChange={setTerminateOpen}
        title={t('enrollments.detail.confirmTerminateTitle')}
        description={t('enrollments.detail.confirmTerminateDesc')}
        destructive
        onConfirm={async () => {
          await terminateMutation.mutateAsync()
        }}
      />

      <SetPriceDialog
        open={priceOpen}
        onOpenChange={setPriceOpen}
        enrollmentId={id}
        currentPrice={toNum(enrollment.coursePrice)}
        onSaved={invalidate}
      />
      <DiscountDialog
        open={discountOpen}
        onOpenChange={setDiscountOpen}
        enrollmentId={id}
        currentDiscount={
          enrollment.discountedPrice != null ? toNum(enrollment.discountedPrice) : null
        }
        onSaved={invalidate}
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

function SetPriceDialog({
  open,
  onOpenChange,
  enrollmentId,
  currentPrice,
  onSaved,
}: {
  open: boolean
  onOpenChange: (v: boolean) => void
  enrollmentId: string
  currentPrice: number
  onSaved: () => void
}) {
  const { t } = useTranslation()
  const {
    register,
    handleSubmit,
    formState: { isSubmitting },
  } = useForm<{ coursePrice: string }>({
    values: { coursePrice: String(currentPrice) },
  })

  async function onSubmit(values: { coursePrice: string }) {
    try {
      await setEnrollmentPrice(enrollmentId, Number(values.coursePrice))
      toast.success(t('enrollments.detail.priceSaved'))
      onSaved()
      onOpenChange(false)
    } catch (err) {
      toast.error(
        err instanceof ApiError ? err.detail || t('common.unknownError') : t('common.networkError'),
      )
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{t('enrollments.detail.setPriceTitle')}</DialogTitle>
        </DialogHeader>
        <form className="flex flex-col gap-4" onSubmit={handleSubmit(onSubmit)} noValidate>
          <Field label={t('enrollments.detail.setPriceLabel')}>
            <Input type="number" min={0} step="0.01" {...register('coursePrice')} />
          </Field>
          <DialogFooter>
            <Button
              type="button"
              variant="outline"
              onClick={() => onOpenChange(false)}
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
  )
}

function DiscountDialog({
  open,
  onOpenChange,
  enrollmentId,
  currentDiscount,
  onSaved,
}: {
  open: boolean
  onOpenChange: (v: boolean) => void
  enrollmentId: string
  currentDiscount: number | null
  onSaved: () => void
}) {
  const { t } = useTranslation()
  const [pending, setPending] = useState(false)
  const { register, handleSubmit } = useForm<{ discountedPrice: string }>({
    values: { discountedPrice: currentDiscount != null ? String(currentDiscount) : '' },
  })

  async function onApply(values: { discountedPrice: string }) {
    if (!values.discountedPrice) return
    setPending(true)
    try {
      await applyEnrollmentDiscount(enrollmentId, Number(values.discountedPrice))
      toast.success(t('enrollments.detail.discountSaved'))
      onSaved()
      onOpenChange(false)
    } catch (err) {
      toast.error(
        err instanceof ApiError ? err.detail || t('common.unknownError') : t('common.networkError'),
      )
    } finally {
      setPending(false)
    }
  }

  async function onRemove() {
    setPending(true)
    try {
      await removeEnrollmentDiscount(enrollmentId)
      toast.success(t('enrollments.detail.discountRemoved'))
      onSaved()
      onOpenChange(false)
    } catch (err) {
      toast.error(
        err instanceof ApiError ? err.detail || t('common.unknownError') : t('common.networkError'),
      )
    } finally {
      setPending(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{t('enrollments.detail.discountTitle')}</DialogTitle>
        </DialogHeader>
        <form className="flex flex-col gap-4" onSubmit={handleSubmit(onApply)} noValidate>
          <Field label={t('enrollments.detail.discountLabel')}>
            <Input type="number" min={0} step="0.01" {...register('discountedPrice')} />
          </Field>
          <DialogFooter className="flex-wrap gap-2">
            {currentDiscount != null && (
              <Button type="button" variant="outline" onClick={onRemove} loading={pending}>
                {t('enrollments.detail.removeDiscount')}
              </Button>
            )}
            <Button
              type="button"
              variant="outline"
              onClick={() => onOpenChange(false)}
              disabled={pending}
            >
              {t('common.cancel')}
            </Button>
            <Button type="submit" loading={pending}>
              {t('enrollments.detail.applyDiscount')}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}

function PreferredScheduleCard({
  enrollmentId,
  value,
  canManage,
}: {
  enrollmentId: string
  value: string | null
  canManage: boolean
}) {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const [text, setText] = useState(value ?? '')
  const [saving, setSaving] = useState(false)

  async function handleSave() {
    setSaving(true)
    try {
      await updateEnrollmentPreferredSchedule(enrollmentId, text || null)
      await queryClient.invalidateQueries({ queryKey: ['enrollments', enrollmentId] })
      toast.success(t('enrollments.detail.preferredScheduleSaved'))
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
        <CardTitle className="text-base font-medium">
          {t('enrollments.detail.preferredScheduleLabel')}
        </CardTitle>
      </CardHeader>
      <CardContent className="flex flex-col gap-3">
        <Textarea
          value={text}
          onChange={(e) => setText(e.target.value)}
          placeholder={t('enrollments.detail.preferredSchedulePlaceholder')}
          disabled={!canManage}
          rows={3}
        />
        {canManage && (
          <Button className="self-start" loading={saving} onClick={handleSave}>
            {t('common.save')}
          </Button>
        )}
      </CardContent>
    </Card>
  )
}

function CommentCard({
  enrollmentId,
  value,
  canManage,
}: {
  enrollmentId: string
  value: string | null
  canManage: boolean
}) {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const [text, setText] = useState(value ?? '')
  const [saving, setSaving] = useState(false)

  async function handleSave() {
    setSaving(true)
    try {
      await updateEnrollmentComment(enrollmentId, text || null)
      await queryClient.invalidateQueries({ queryKey: ['enrollments', enrollmentId] })
      toast.success(t('enrollments.detail.commentSaved'))
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
        <CardTitle className="text-base font-medium">
          {t('enrollments.detail.commentLabel')}
        </CardTitle>
      </CardHeader>
      <CardContent className="flex flex-col gap-3">
        <Textarea
          value={text}
          onChange={(e) => setText(e.target.value)}
          placeholder={t('enrollments.detail.commentPlaceholder')}
          disabled={!canManage}
          rows={4}
        />
        {canManage && (
          <Button className="self-start" loading={saving} onClick={handleSave}>
            {t('common.save')}
          </Button>
        )}
      </CardContent>
    </Card>
  )
}
