import { useTranslation } from 'react-i18next'
import { useQuery } from '@tanstack/react-query'
import { format } from 'date-fns'
import { uk } from 'date-fns/locale'
import { useAuth } from '@/features/auth/AuthProvider'
import { getMyStudentProfile, getMyTeacherProfile } from '@/features/profile/api'
import { enumLabel } from '@/lib/enumLabels'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Skeleton } from '@/components/ui/skeleton'

function Field({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div className="flex flex-col gap-0.5">
      <span className="text-xs text-muted-foreground">{label}</span>
      <span className="text-sm">{value}</span>
    </div>
  )
}

function formatDate(iso: string) {
  try {
    return format(new Date(iso), 'dd.MM.yyyy', { locale: uk })
  } catch {
    return iso
  }
}

function StudentProfileCard() {
  const { t } = useTranslation()
  const { data, isPending } = useQuery({
    queryKey: ['profile', 'student', 'me'],
    queryFn: getMyStudentProfile,
  })

  if (isPending) return <Skeleton className="h-64 w-full" />
  if (!data) return null

  return (
    <div className="flex flex-col gap-4">
      <Card>
        <CardHeader>
          <CardTitle className="text-base font-medium">{t('profile.tabs.profileData')}</CardTitle>
        </CardHeader>
        <CardContent className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <Field
            label={t('profile.studentData.fullName')}
            value={`${data.lastName} ${data.firstName} ${data.middleName ?? ''}`.trim()}
          />
          <Field
            label={t('profile.studentData.dateOfBirth')}
            value={formatDate(data.dateOfBirth)}
          />
          <Field label={t('profile.studentData.phone')} value={data.phoneNumber} />
          <Field label={t('profile.studentData.email')} value={data.email} />
          <Field label={t('profile.studentData.city')} value={data.city} />
          <Field label={t('profile.studentData.country')} value={data.country} />
          <Field
            label={t('profile.studentData.status')}
            value={<Badge variant="secondary">{enumLabel(t, 'studentStatus', data.status)}</Badge>}
          />
          <Field
            label={t('profile.studentData.languages')}
            value={data.languages.map((l) => enumLabel(t, 'language', l)).join(', ') || '—'}
          />
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-base font-medium">
            {t('profile.studentData.preferencesTitle')}
          </CardTitle>
        </CardHeader>
        <CardContent className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <Field
            label={t('profile.studentData.learningGoal')}
            value={enumLabel(t, 'learningGoal', data.preferences.learningGoal)}
          />
          <Field
            label={t('profile.studentData.format')}
            value={enumLabel(t, 'format', data.preferences.format)}
          />
          <Field
            label={t('profile.studentData.lessonType')}
            value={enumLabel(t, 'lessonType', data.preferences.lessonType)}
          />
          <Field label={t('profile.studentData.intensity')} value={data.preferences.intensity} />
          <Field
            label={t('profile.studentData.currentLevel')}
            value={enumLabel(t, 'level', data.preferences.currentLevel)}
          />
          <Field
            label={t('profile.studentData.hadPreviousCourses')}
            value={data.preferences.hadPreviousCourses ? t('common.yes') : t('common.no')}
          />
        </CardContent>
      </Card>

      {data.isChild && (
        <Card>
          <CardHeader>
            <CardTitle className="text-base font-medium">
              {t('profile.studentData.parentTitle')}
            </CardTitle>
          </CardHeader>
          <CardContent>
            {data.parentInfo ? (
              <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                <Field
                  label={t('profile.studentData.fullName')}
                  value={`${data.parentInfo.lastName} ${data.parentInfo.firstName} ${data.parentInfo.middleName ?? ''}`.trim()}
                />
                <Field label={t('profile.studentData.phone')} value={data.parentInfo.phoneNumber} />
                <Field label={t('profile.studentData.email')} value={data.parentInfo.email} />
              </div>
            ) : (
              <p className="text-sm text-muted-foreground">{t('profile.studentData.noParent')}</p>
            )}
          </CardContent>
        </Card>
      )}

      <p className="text-xs text-muted-foreground">{t('profile.studentData.editNote')}</p>
    </div>
  )
}

function TeacherProfileCard() {
  const { t } = useTranslation()
  const { data, isPending } = useQuery({
    queryKey: ['profile', 'teacher', 'me'],
    queryFn: getMyTeacherProfile,
  })

  if (isPending) return <Skeleton className="h-64 w-full" />
  if (!data) return null

  return (
    <div className="flex flex-col gap-4">
      <Card>
        <CardHeader>
          <CardTitle className="text-base font-medium">{t('profile.tabs.profileData')}</CardTitle>
        </CardHeader>
        <CardContent className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <Field
            label={t('profile.teacherData.fullName')}
            value={`${data.lastName} ${data.firstName} ${data.middleName ?? ''}`.trim()}
          />
          <Field
            label={t('profile.teacherData.dateOfBirth')}
            value={formatDate(data.dateOfBirth)}
          />
          <Field label={t('profile.teacherData.phone')} value={data.phoneNumber} />
          <Field label={t('profile.teacherData.email')} value={data.email} />
          <Field label={t('profile.teacherData.city')} value={data.city} />
          <Field label={t('profile.teacherData.country')} value={data.country} />
          <Field
            label={t('profile.teacherData.status')}
            value={<Badge variant="secondary">{enumLabel(t, 'teacherStatus', data.status)}</Badge>}
          />
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-base font-medium">
            {t('profile.teacherData.salaryTitle')}
          </CardTitle>
        </CardHeader>
        <CardContent className="flex flex-col gap-3">
          {data.currentSalaryRate ? (
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
              <Field
                label={t('profile.teacherData.baseSalary')}
                value={data.currentSalaryRate.baseSalary}
              />
              <Field
                label={t('profile.teacherData.lessonsRate')}
                value={data.currentSalaryRate.lessonsRate}
              />
              <Field
                label={t('profile.teacherData.effectiveFrom')}
                value={formatDate(data.currentSalaryRate.effectiveFrom)}
              />
            </div>
          ) : (
            <p className="text-sm text-muted-foreground">{t('profile.teacherData.noRates')}</p>
          )}
        </CardContent>
      </Card>

      <p className="text-xs text-muted-foreground">{t('profile.teacherData.editNote')}</p>
    </div>
  )
}

export function ProfileDataTab() {
  const { user } = useAuth()
  if (user?.profile?.type === 'Student') return <StudentProfileCard />
  if (user?.profile?.type === 'Teacher') return <TeacherProfileCard />
  return null
}
