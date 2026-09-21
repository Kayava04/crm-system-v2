import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { Pencil } from 'lucide-react'
import { getMyEnrollments, updateEnrollmentPreferredSchedule } from '@/features/enrollments/api'
import type { MyEnrollment } from '@/features/enrollments/api'
import { ApiError } from '@/api/errors'
import { enumLabel } from '@/lib/enumLabels'
import { formatCurrency, formatDate } from '@/lib/utils'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Textarea } from '@/components/ui/textarea'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'

function statusVariant(status: string): 'success' | 'warning' | 'secondary' | 'destructive' {
  if (status === 'Active') return 'success'
  if (status === 'Suspended') return 'warning'
  if (status === 'Completed') return 'secondary'
  return 'destructive'
}

export function MyEnrollmentsPage() {
  const { t, i18n } = useTranslation()
  const lang = i18n.language === 'en' ? 'en' : 'uk'

  const { data: enrollments, isLoading } = useQuery({
    queryKey: ['enrollments', 'my'],
    queryFn: getMyEnrollments,
  })

  return (
    <div className="flex max-w-2xl flex-col gap-4">
      <h1 className="text-2xl font-semibold tracking-tight">{t('nav.enrollments')}</h1>

      {isLoading && (
        <div className="flex flex-col gap-3">
          <Skeleton className="h-24 w-full" />
          <Skeleton className="h-24 w-full" />
        </div>
      )}

      {enrollments && enrollments.length === 0 && (
        <p className="text-sm text-muted-foreground">{t('students.detail.noEnrollments')}</p>
      )}

      <div className="flex flex-col gap-3">
        {enrollments?.map((e) => (
          <MyEnrollmentCard key={e.id} enrollment={e} lang={lang} />
        ))}
      </div>
    </div>
  )
}

function MyEnrollmentCard({ enrollment, lang }: { enrollment: MyEnrollment; lang: string }) {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const [editingSchedule, setEditingSchedule] = useState(false)
  const [scheduleText, setScheduleText] = useState('')
  const [saving, setSaving] = useState(false)

  async function handleSaveSchedule() {
    setSaving(true)
    try {
      await updateEnrollmentPreferredSchedule(enrollment.id, scheduleText || null)
      await queryClient.invalidateQueries({ queryKey: ['enrollments', 'my'] })
      toast.success(t('enrollments.detail.preferredScheduleSaved'))
      setEditingSchedule(false)
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
      <CardHeader className="flex flex-row items-center justify-between">
        <CardTitle className="text-base font-medium">{enrollment.courseName}</CardTitle>
        <Badge variant={statusVariant(enrollment.status)}>
          {enumLabel(t, 'enrollmentStatus', enrollment.status)}
        </Badge>
      </CardHeader>
      <CardContent className="flex flex-col gap-3">
        <div className="flex flex-wrap gap-x-6 gap-y-1 text-sm text-muted-foreground">
          <span>
            {formatDate(enrollment.startDate, lang)} – {formatDate(enrollment.endDate, lang)}
          </span>
          <span>{formatCurrency(enrollment.effectivePrice, lang)}</span>
        </div>

        {editingSchedule ? (
          <div className="flex flex-col gap-2">
            <Textarea
              value={scheduleText}
              onChange={(e) => setScheduleText(e.target.value)}
              placeholder={t('enrollments.detail.preferredSchedulePlaceholder')}
              rows={2}
            />
            <div className="flex gap-2">
              <Button size="sm" loading={saving} onClick={handleSaveSchedule}>
                {t('common.save')}
              </Button>
              <Button
                size="sm"
                variant="outline"
                onClick={() => setEditingSchedule(false)}
                disabled={saving}
              >
                {t('common.cancel')}
              </Button>
            </div>
          </div>
        ) : (
          <Button
            variant="ghost"
            size="sm"
            className="w-fit"
            onClick={() => {
              setScheduleText('')
              setEditingSchedule(true)
            }}
          >
            <Pencil />
            {t('enrollments.detail.preferredScheduleLabel')}
          </Button>
        )}
      </CardContent>
    </Card>
  )
}
