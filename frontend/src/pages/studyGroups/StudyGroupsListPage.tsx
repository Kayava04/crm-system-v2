import { useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { useQuery, keepPreviousData } from '@tanstack/react-query'
import { Plus } from 'lucide-react'
import { getGroups } from '@/features/studyGroups/api'
import { useResolvedGroups, type ResolvedGroupRow } from '@/features/studyGroups/useResolvedGroups'
import { getAllCoursesForLookup } from '@/features/courses/api'
import { toNum } from '@/lib/utils'
import { Button } from '@/components/ui/button'
import { DataTable, type DataTableColumn } from '@/components/shared/DataTable'
import { Pagination } from '@/components/shared/Pagination'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { TeacherSearchInput } from '@/components/shared/TeacherSearchInput'
import { useCan } from '@/features/auth/useCan'

export function StudyGroupsListPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const canCreate = useCan('CanManageSchedule')

  const [searchParams, setSearchParams] = useSearchParams()
  const page = Number(searchParams.get('page') ?? '1')
  const courseId = searchParams.get('courseId') ?? ''
  const teacherId = searchParams.get('teacherId') ?? ''
  const teacherName = searchParams.get('teacherName') ?? ''

  const { data: courses } = useQuery({
    queryKey: ['courses', 'lookup-all'],
    queryFn: getAllCoursesForLookup,
    staleTime: 5 * 60_000,
  })

  const filters = useMemo(
    () => ({
      courseId: courseId || undefined,
      teacherId: teacherId || undefined,
      page,
      pageSize: 20,
    }),
    [courseId, teacherId, page],
  )

  const { data, isPending, isPlaceholderData } = useQuery({
    queryKey: ['study-groups', filters],
    queryFn: () => getGroups(filters),
    placeholderData: keepPreviousData,
  })

  const resolved = useResolvedGroups(data?.items ?? [])

  function updateParams(next: Record<string, string>) {
    const params = new URLSearchParams(searchParams)
    for (const [key, value] of Object.entries(next)) {
      if (value) params.set(key, value)
      else params.delete(key)
    }
    params.delete('page')
    setSearchParams(params, { replace: true })
  }

  function goToPage(nextPage: number) {
    const params = new URLSearchParams(searchParams)
    params.set('page', String(nextPage))
    setSearchParams(params, { replace: true })
  }

  const hasFilters = !!(courseId || teacherId)

  const columns: DataTableColumn<ResolvedGroupRow>[] = [
    {
      key: 'name',
      header: t('studyGroups.columns.name'),
      cell: (row) => <span className="font-medium">{row.row.name}</span>,
    },
    {
      key: 'course',
      header: t('studyGroups.columns.course'),
      cell: (row) => row.courseName ?? '—',
    },
    {
      key: 'teacher',
      header: t('studyGroups.columns.teacher'),
      cell: (row) => row.teacherName ?? '—',
    },
    {
      key: 'members',
      header: t('studyGroups.columns.members'),
      cell: (row) => Number(row.row.membersCount),
    },
  ]

  return (
    <div className="flex flex-col gap-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-2xl font-semibold tracking-tight">{t('studyGroups.title')}</h1>
        {canCreate && (
          <Button onClick={() => navigate('/study-groups/new')}>
            <Plus />
            {t('studyGroups.add')}
          </Button>
        )}
      </div>

      <div className="flex flex-wrap items-end gap-3">
        <Select
          value={courseId}
          onValueChange={(v) => updateParams({ courseId: v === 'any' ? '' : v })}
        >
          <SelectTrigger className="w-48">
            <SelectValue placeholder={t('studyGroups.filters.course')} />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="any">{t('studyGroups.filters.any')}</SelectItem>
            {courses?.map((c) => (
              <SelectItem key={c.id} value={c.id}>
                {c.name}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        <div className="min-w-48">
          <TeacherSearchInput
            value={teacherId ? { id: teacherId, fullName: teacherName } : null}
            onChange={(v) =>
              updateParams({ teacherId: v?.id ?? '', teacherName: v?.fullName ?? '' })
            }
            placeholder={t('studyGroups.filters.teacher')}
          />
        </div>
        {hasFilters && (
          <Button variant="ghost" onClick={() => setSearchParams({}, { replace: true })}>
            {t('studyGroups.filters.clear')}
          </Button>
        )}
      </div>

      <div className={isPlaceholderData ? 'opacity-60 transition-opacity' : undefined}>
        <DataTable
          columns={columns}
          rows={resolved}
          rowKey={(row) => row.row.id}
          isLoading={isPending}
          onRowClick={(row) => navigate(`/study-groups/${row.row.id}`)}
        />
      </div>

      {data && (
        <Pagination
          page={toNum(data.page)}
          totalPages={toNum(data.totalPages)}
          totalCount={toNum(data.totalCount)}
          onPageChange={goToPage}
        />
      )}
    </div>
  )
}
