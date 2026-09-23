import { useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { useQuery, keepPreviousData } from '@tanstack/react-query'
import { Plus } from 'lucide-react'
import { getCourses } from '@/features/courses/api'
import type { CourseListItem } from '@/features/courses/api'
import { enumLabel } from '@/lib/enumLabels'
import { toNum, formatCurrency } from '@/lib/utils'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { DataTable, type DataTableColumn } from '@/components/shared/DataTable'
import { Pagination } from '@/components/shared/Pagination'
import { useDebouncedValue } from '@/lib/useDebouncedValue'
import { useCan } from '@/features/auth/useCan'

const LANGUAGES = ['English', 'French', 'German', 'Polish', 'Spanish', 'Italian'] as const
const LEVELS = ['A1', 'A2', 'B1', 'B2', 'C1', 'C2'] as const
const FORMATS = ['Online', 'Offline'] as const
const LESSON_TYPES = ['Individual', 'Group'] as const
const STATUSES = ['Active', 'Archived'] as const

function statusVariant(status: string): 'success' | 'secondary' {
  return status === 'Active' ? 'success' : 'secondary'
}

export function CoursesListPage() {
  const { t, i18n } = useTranslation()
  const lang = i18n.language === 'en' ? 'en' : 'uk'
  const navigate = useNavigate()
  const canCreate = useCan('CanManageCourses')

  const [searchParams, setSearchParams] = useSearchParams()
  const [searchInput, setSearchInput] = useState(searchParams.get('search') ?? '')
  const debouncedSearch = useDebouncedValue(searchInput)

  const page = Number(searchParams.get('page') ?? '1')
  const language = searchParams.get('language') ?? ''
  const level = searchParams.get('level') ?? ''
  const format = searchParams.get('format') ?? ''
  const lessonType = searchParams.get('lessonType') ?? ''
  const status = searchParams.get('status') ?? ''

  const filters = useMemo(
    () => ({
      search: debouncedSearch || undefined,
      language: (language || undefined) as never,
      level: (level || undefined) as never,
      format: (format || undefined) as never,
      lessonType: (lessonType || undefined) as never,
      status: (status || undefined) as never,
      page,
      pageSize: 20,
    }),
    [debouncedSearch, language, level, format, lessonType, status, page],
  )

  const { data, isPending, isPlaceholderData } = useQuery({
    queryKey: ['courses', filters],
    queryFn: () => getCourses(filters),
    placeholderData: keepPreviousData,
  })

  function updateParam(key: string, value: string) {
    const next = new URLSearchParams(searchParams)
    if (value) next.set(key, value)
    else next.delete(key)
    next.delete('page')
    setSearchParams(next, { replace: true })
  }

  function goToPage(nextPage: number) {
    const next = new URLSearchParams(searchParams)
    next.set('page', String(nextPage))
    setSearchParams(next, { replace: true })
  }

  function clearFilters() {
    setSearchInput('')
    setSearchParams({}, { replace: true })
  }

  const hasFilters = !!(language || level || format || lessonType || status || debouncedSearch)

  const columns: DataTableColumn<CourseListItem>[] = [
    {
      key: 'name',
      header: t('courses.columns.name'),
      cell: (row) => <span className="font-medium">{row.name}</span>,
    },
    {
      key: 'language',
      header: t('courses.columns.language'),
      cell: (row) => enumLabel(t, 'language', row.language),
    },
    {
      key: 'level',
      header: t('courses.columns.level'),
      cell: (row) => enumLabel(t, 'level', row.level),
    },
    {
      key: 'price',
      header: t('courses.columns.price'),
      cell: (row) => formatCurrency(row.price, lang),
    },
    {
      key: 'status',
      header: t('courses.columns.status'),
      cell: (row) => (
        <Badge variant={statusVariant(row.status)}>
          {enumLabel(t, 'courseStatus', row.status)}
        </Badge>
      ),
    },
  ]

  return (
    <div className="flex flex-col gap-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-2xl font-semibold tracking-tight">{t('courses.title')}</h1>
        {canCreate && (
          <Button onClick={() => navigate('/courses/new')}>
            <Plus />
            {t('courses.add')}
          </Button>
        )}
      </div>

      <div className="flex flex-wrap items-end gap-3">
        <div className="flex min-w-56 flex-1 flex-col gap-1.5">
          <Input
            placeholder={t('courses.searchPlaceholder')}
            value={searchInput}
            onChange={(e) => {
              setSearchInput(e.target.value)
              updateParam('search', e.target.value)
            }}
          />
        </div>
        <Select
          value={language}
          onValueChange={(v) => updateParam('language', v === 'any' ? '' : v)}
        >
          <SelectTrigger className="w-36">
            <SelectValue placeholder={t('courses.filters.language')} />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="any">{t('courses.filters.any')}</SelectItem>
            {LANGUAGES.map((l) => (
              <SelectItem key={l} value={l}>
                {t(`enums.language.${l}`)}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        <Select value={level} onValueChange={(v) => updateParam('level', v === 'any' ? '' : v)}>
          <SelectTrigger className="w-28">
            <SelectValue placeholder={t('courses.filters.level')} />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="any">{t('courses.filters.any')}</SelectItem>
            {LEVELS.map((l) => (
              <SelectItem key={l} value={l}>
                {l}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        <Select value={format} onValueChange={(v) => updateParam('format', v === 'any' ? '' : v)}>
          <SelectTrigger className="w-32">
            <SelectValue placeholder={t('courses.filters.format')} />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="any">{t('courses.filters.any')}</SelectItem>
            {FORMATS.map((f) => (
              <SelectItem key={f} value={f}>
                {t(`enums.format.${f}`)}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        <Select
          value={lessonType}
          onValueChange={(v) => updateParam('lessonType', v === 'any' ? '' : v)}
        >
          <SelectTrigger className="w-36">
            <SelectValue placeholder={t('courses.filters.lessonType')} />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="any">{t('courses.filters.any')}</SelectItem>
            {LESSON_TYPES.map((v) => (
              <SelectItem key={v} value={v}>
                {t(`enums.lessonType.${v}`)}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        <Select value={status} onValueChange={(v) => updateParam('status', v === 'any' ? '' : v)}>
          <SelectTrigger className="w-32">
            <SelectValue placeholder={t('courses.filters.status')} />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="any">{t('courses.filters.any')}</SelectItem>
            {STATUSES.map((s) => (
              <SelectItem key={s} value={s}>
                {enumLabel(t, 'courseStatus', s)}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        {hasFilters && (
          <Button variant="ghost" onClick={clearFilters}>
            {t('courses.filters.clear')}
          </Button>
        )}
      </div>

      <div className={isPlaceholderData ? 'opacity-60 transition-opacity' : undefined}>
        <DataTable
          columns={columns}
          rows={data?.items ?? []}
          rowKey={(row) => row.id}
          isLoading={isPending}
          onRowClick={(row) => navigate(`/courses/${row.id}`)}
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
