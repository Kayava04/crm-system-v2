import { useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useQuery } from '@tanstack/react-query'
import { getMyCalendar } from '@/features/scheduling/api'
import type { CalendarItem } from '@/features/scheduling/api'
import { formatDate, formatDateTime } from '@/lib/utils'
import { enumLabel } from '@/lib/enumLabels'
import { Badge } from '@/components/ui/badge'
import { Input } from '@/components/ui/input'
import { Skeleton } from '@/components/ui/skeleton'
import { DataTable, type DataTableColumn } from '@/components/shared/DataTable'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'

interface StudentRosterRow {
  name: string
  courses: Set<string>
  lessonCount: number
  nextLessonAt: string | null
  lastLessonAt: string | null
}

function toIsoDate(d: Date): string {
  const year = d.getFullYear()
  const month = String(d.getMonth() + 1).padStart(2, '0')
  const day = String(d.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}

function statusVariant(status: string): 'success' | 'secondary' | 'destructive' {
  if (status === 'Cancelled') return 'destructive'
  if (status === 'Completed') return 'success'
  return 'secondary'
}

export function MyStudentsPage() {
  const { t, i18n } = useTranslation()
  const lang = i18n.language === 'en' ? 'en' : 'uk'
  const [search, setSearch] = useState('')
  const [courseFilter, setCourseFilter] = useState('')
  const [selectedStudent, setSelectedStudent] = useState<string | null>(null)

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

  const allCourseNames = useMemo(() => {
    const names = new Set<string>()
    for (const item of data?.items ?? []) names.add(item.courseName)
    return Array.from(names).sort((a, b) => a.localeCompare(b, lang))
  }, [data, lang])

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

  const filteredRoster = useMemo(() => {
    return roster.filter((row) => {
      if (search && !row.name.toLowerCase().includes(search.toLowerCase())) return false
      if (courseFilter && !row.courses.has(courseFilter)) return false
      return true
    })
  }, [roster, search, courseFilter])

  const selectedStudentLessons = useMemo(() => {
    if (!selectedStudent) return []
    return (data?.items ?? [])
      .filter((item) => item.students.includes(selectedStudent))
      .sort((a, b) => a.startsAt.localeCompare(b.startsAt))
  }, [data, selectedStudent])

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

  const hasFilters = !!(search || courseFilter)

  return (
    <div className="flex flex-col gap-4">
      <h1 className="text-2xl font-semibold tracking-tight">{t('nav.myStudents')}</h1>

      <div className="flex flex-wrap items-end gap-3">
        <div className="min-w-48">
          <Input
            placeholder={t('myStudents.searchPlaceholder')}
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
        </div>
        <Select value={courseFilter} onValueChange={(v) => setCourseFilter(v === 'any' ? '' : v)}>
          <SelectTrigger className="w-48">
            <SelectValue placeholder={t('myStudents.filters.course')} />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="any">{t('myStudents.filters.any')}</SelectItem>
            {allCourseNames.map((name) => (
              <SelectItem key={name} value={name}>
                {name}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        {hasFilters && (
          <Button
            variant="ghost"
            size="sm"
            onClick={() => {
              setSearch('')
              setCourseFilter('')
            }}
          >
            {t('myStudents.filters.clear')}
          </Button>
        )}
      </div>

      {isLoading ? (
        <Skeleton className="h-40 w-full" />
      ) : (
        <DataTable
          columns={columns}
          rows={filteredRoster}
          rowKey={(row) => row.name}
          emptyMessage={t('myStudents.empty')}
          onRowClick={(row) => setSelectedStudent(row.name)}
        />
      )}

      <StudentLessonsDialog
        open={!!selectedStudent}
        onOpenChange={(open) => !open && setSelectedStudent(null)}
        studentName={selectedStudent}
        lessons={selectedStudentLessons}
        lang={lang}
      />
    </div>
  )
}

function StudentLessonsDialog({
  open,
  onOpenChange,
  studentName,
  lessons,
  lang,
}: {
  open: boolean
  onOpenChange: (open: boolean) => void
  studentName: string | null
  lessons: CalendarItem[]
  lang: string
}) {
  const { t } = useTranslation()
  if (!studentName) return null

  const columns: DataTableColumn<CalendarItem>[] = [
    {
      key: 'date',
      header: t('myStudents.detail.columns.date'),
      cell: (row) => formatDateTime(row.startsAt, lang),
    },
    {
      key: 'course',
      header: t('myStudents.detail.columns.course'),
      cell: (row) => row.courseName,
    },
    {
      key: 'status',
      header: t('myStudents.detail.columns.status'),
      cell: (row) => (
        <Badge variant={statusVariant(row.status)}>
          {enumLabel(t, 'scheduleStatus', row.status)}
        </Badge>
      ),
    },
  ]

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>{t('myStudents.detail.title', { name: studentName })}</DialogTitle>
        </DialogHeader>
        <DataTable
          columns={columns}
          rows={lessons}
          rowKey={(row) => row.id}
          emptyMessage={t('myStudents.detail.empty')}
        />
        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            {t('common.close')}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
