import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import { useResolvedEnrollments } from '@/features/enrollments/useResolvedEnrollments'
import type { EnrollmentListItem } from '@/features/enrollments/api'
import { enumLabel } from '@/lib/enumLabels'
import { formatCurrency, formatDate } from '@/lib/utils'
import { Badge } from '@/components/ui/badge'
import { Skeleton } from '@/components/ui/skeleton'

function statusVariant(status: string): 'success' | 'warning' | 'secondary' | 'destructive' {
  if (status === 'Active') return 'success'
  if (status === 'Suspended') return 'warning'
  if (status === 'Completed') return 'secondary'
  return 'destructive'
}

interface EnrollmentsTableProps {
  rows: EnrollmentListItem[]
  hideColumn?: 'student' | 'course'
}

/** Renders enrollment rows with the student/course name resolved client-side
 * (see useResolvedEnrollments for why that resolution is needed at all). */
export function EnrollmentsTable({ rows, hideColumn }: EnrollmentsTableProps) {
  const { t, i18n } = useTranslation()
  const lang = i18n.language === 'en' ? 'en' : 'uk'
  const navigate = useNavigate()
  const resolved = useResolvedEnrollments(rows)

  if (rows.length === 0) return null

  return (
    <div className="overflow-x-auto rounded-md border border-border">
      <table className="w-full text-sm">
        <thead className="bg-muted/50 text-left text-xs text-muted-foreground">
          <tr>
            <th className="px-3 py-2 font-medium">{t('enrollments.columns.number')}</th>
            {hideColumn !== 'student' && (
              <th className="px-3 py-2 font-medium">{t('enrollments.columns.student')}</th>
            )}
            {hideColumn !== 'course' && (
              <th className="px-3 py-2 font-medium">{t('enrollments.columns.course')}</th>
            )}
            <th className="px-3 py-2 font-medium">{t('enrollments.columns.dates')}</th>
            <th className="px-3 py-2 font-medium">{t('enrollments.columns.price')}</th>
            <th className="px-3 py-2 font-medium">{t('enrollments.columns.status')}</th>
          </tr>
        </thead>
        <tbody>
          {resolved.map(({ row, studentName, courseName, isResolving }) => (
            <tr
              key={row.id}
              className="cursor-pointer border-t border-border hover:bg-muted/40"
              onClick={() => navigate(`/enrollments/${row.id}`)}
            >
              <td className="px-3 py-2 font-medium">{row.enrollmentNumber}</td>
              {hideColumn !== 'student' && (
                <td className="px-3 py-2">
                  {isResolving ? <Skeleton className="h-4 w-24" /> : (studentName ?? '—')}
                </td>
              )}
              {hideColumn !== 'course' && (
                <td className="px-3 py-2">
                  {isResolving ? <Skeleton className="h-4 w-24" /> : (courseName ?? '—')}
                </td>
              )}
              <td className="px-3 py-2 whitespace-nowrap text-muted-foreground">
                {formatDate(row.startDate, lang)} – {formatDate(row.endDate, lang)}
              </td>
              <td className="px-3 py-2 whitespace-nowrap">
                {formatCurrency(row.effectivePrice, lang)}
              </td>
              <td className="px-3 py-2">
                <Badge variant={statusVariant(row.status)}>
                  {enumLabel(t, 'enrollmentStatus', row.status)}
                </Badge>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
