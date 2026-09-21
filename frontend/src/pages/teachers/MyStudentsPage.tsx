import { useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useQuery } from '@tanstack/react-query'
import { getMyCalendar } from '@/features/scheduling/api'
import { formatDate } from '@/lib/utils'
import { Card, CardContent } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import { DataTable, type DataTableColumn } from '@/components/shared/DataTable'

interface StudentRosterRow {
  name: string
  courses: Set<string>
  lessonCount: number
  nextLessonAt: string | null
  lastLessonAt: string | null
}

function toIsoDate(d: Date): string {
  return d.toISOString().slice(0, 10)
}

export function MyStudentsPage() {
  const { t, i18n } = useTranslation()
  const lang = i18n.language === 'en' ? 'en' : 'uk'

  const { from, to } = useMemo(() => {
    const now = new Date()
    const from = new Date(now)
    from.setDate(from.getDate() - 90)
    const to = new Date(now)
    to.setDate(to.getDate() + 90)
    return { from: toIsoDate(from), to: toIsoDate(to) }
  }, [])

  const { data, isLoading } = useQuery({
    queryKey: ['calendar', 'my', 'roster', from, to],
    queryFn: () => getMyCalendar(from, to),
  })

  const [now] = useState(() => Date.now())

  const roster = useMemo<StudentRosterRow[]>(() => {
    const byName = new Map<string, StudentRosterRow>()
    for (const item of data?.items ?? []) {
      for (const studentName of item.students) {
        const row = byName.get(studentName) ?? {
          name: studentName,
          courses: new Set<string>(),
          lessonCount: 0,
          nextLessonAt: null,
          lastLessonAt: null,
        }
        row.courses.add(item.courseName)
        row.lessonCount += 1
        const startsAt = item.startsAt
        if (new Date(startsAt).getTime() >= now) {
          if (!row.nextLessonAt || startsAt < row.nextLessonAt) row.nextLessonAt = startsAt
        } else {
          if (!row.lastLessonAt || startsAt > row.lastLessonAt) row.lastLessonAt = startsAt
        }
        byName.set(studentName, row)
      }
    }
    return Array.from(byName.values()).sort((a, b) => a.name.localeCompare(b.name, lang))
  }, [data, lang, now])

  const columns: DataTableColumn<StudentRosterRow>[] = [
    {
      key: 'name',
      header: t('myStudents.columns.name'),
      cell: (row) => <span className="font-medium">{row.name}</span>,
    },
    {
      key: 'courses',
      header: t('myStudents.columns.courses'),
      cell: (row) => Array.from(row.courses).join(', '),
    },
    {
      key: 'lessons',
      header: t('myStudents.columns.lessons'),
      cell: (row) => row.lessonCount,
    },
    {
      key: 'next',
      header: t('myStudents.columns.nextLesson'),
      cell: (row) =>
        row.nextLessonAt
          ? formatDate(row.nextLessonAt, lang)
          : row.lastLessonAt
            ? `${t('myStudents.columns.lastLesson')}: ${formatDate(row.lastLessonAt, lang)}`
            : '—',
    },
  ]

  return (
    <div className="flex flex-col gap-4">
      <h1 className="text-2xl font-semibold tracking-tight">{t('nav.myStudents')}</h1>
      <Card>
        <CardContent className="py-3">
          <p className="text-xs text-muted-foreground">{t('myStudents.note')}</p>
        </CardContent>
      </Card>

      {isLoading ? (
        <Skeleton className="h-40 w-full" />
      ) : (
        <DataTable
          columns={columns}
          rows={roster}
          rowKey={(row) => row.name}
          emptyMessage={t('myStudents.empty')}
        />
      )}
    </div>
  )
}
