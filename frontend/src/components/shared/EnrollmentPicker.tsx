import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useQuery } from '@tanstack/react-query'
import { getStudents } from '@/features/students/api'
import { getEnrollments } from '@/features/enrollments/api'
import { enumLabel } from '@/lib/enumLabels'
import { useDebouncedValue } from '@/lib/useDebouncedValue'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'

interface EnrollmentPickerProps {
  value: { id: string; label: string } | null
  onChange: (value: { id: string; label: string } | null) => void
  studentSearchPlaceholder?: string
  /** Restrict results to enrollments in this course (e.g. a study group's course). */
  courseId?: string
  /** Overrides the "no results" message shown once a student is picked but has no
   * (matching, when courseId is set) enrollments. */
  emptyMessage?: string
}

/** Search a student, then pick one of their enrollments by id. Used wherever
 * a dialog needs a specific enrollmentId (there is no direct "search
 * enrollments" endpoint, so this goes through the student first). */
export function EnrollmentPicker({
  value,
  onChange,
  studentSearchPlaceholder,
  courseId,
  emptyMessage,
}: EnrollmentPickerProps) {
  const { t } = useTranslation()
  const [query, setQuery] = useState('')
  const debouncedQuery = useDebouncedValue(query)
  const [pickedStudent, setPickedStudent] = useState<{ id: string; fullName: string } | null>(null)

  const { data: studentResults } = useQuery({
    queryKey: ['students', 'enrollment-picker-search', debouncedQuery],
    queryFn: () => getStudents({ search: debouncedQuery, page: 1, pageSize: 10 }),
    enabled: !pickedStudent && !value && debouncedQuery.length >= 2,
  })

  const { data: enrollments } = useQuery({
    queryKey: ['enrollments', { studentId: pickedStudent?.id, courseId }],
    queryFn: () => getEnrollments({ studentId: pickedStudent!.id, courseId, pageSize: 50 }),
    enabled: !!pickedStudent && !value,
  })

  if (value) {
    return (
      <div className="flex items-center justify-between rounded-md border border-input px-3 py-2 text-sm">
        <span>{value.label}</span>
        <button
          type="button"
          className="text-xs text-muted-foreground hover:text-foreground"
          onClick={() => {
            onChange(null)
            setPickedStudent(null)
          }}
        >
          {t('common.cancel')}
        </button>
      </div>
    )
  }

  if (pickedStudent) {
    return (
      <div className="flex flex-col gap-1">
        <div className="flex items-center justify-between text-xs text-muted-foreground">
          <span>{pickedStudent.fullName}</span>
          <button
            type="button"
            className="hover:text-foreground"
            onClick={() => setPickedStudent(null)}
          >
            {t('common.cancel')}
          </button>
        </div>
        <div className="flex flex-col gap-1 rounded-md border border-border">
          {enrollments?.items.map((e) => (
            <button
              key={e.id}
              type="button"
              className="flex items-center justify-between px-3 py-1.5 text-left text-sm hover:bg-muted"
              onClick={() =>
                onChange({ id: e.id, label: `${pickedStudent.fullName} — ${e.enrollmentNumber}` })
              }
            >
              <span>{e.enrollmentNumber}</span>
              <Badge variant="secondary">{enumLabel(t, 'enrollmentStatus', e.status)}</Badge>
            </button>
          ))}
          {enrollments && enrollments.items.length === 0 && (
            <p className="px-3 py-1.5 text-sm text-muted-foreground">{emptyMessage ?? '—'}</p>
          )}
        </div>
      </div>
    )
  }

  return (
    <div className="flex flex-col gap-1">
      <Input
        value={query}
        onChange={(e) => setQuery(e.target.value)}
        placeholder={studentSearchPlaceholder}
      />
      {studentResults && studentResults.items.length > 0 && (
        <div className="flex flex-col rounded-md border border-border">
          {studentResults.items.map((s) => (
            <button
              key={s.id}
              type="button"
              className="px-3 py-1.5 text-left text-sm hover:bg-muted"
              onClick={() => {
                setPickedStudent({ id: s.id, fullName: s.fullName })
                setQuery('')
              }}
            >
              {s.fullName} <span className="text-xs text-muted-foreground">{s.email}</span>
            </button>
          ))}
        </div>
      )}
    </div>
  )
}
