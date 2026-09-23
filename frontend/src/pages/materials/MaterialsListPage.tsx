import { useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useSearchParams } from 'react-router-dom'
import { useQuery, useQueryClient, keepPreviousData } from '@tanstack/react-query'
import { Plus, Video, FileText, Link as LinkIcon } from 'lucide-react'
import { getMaterials } from '@/features/materials/api'
import type { MaterialListItem, MaterialType } from '@/features/materials/api'
import { getAllCoursesForLookup } from '@/features/courses/api'
import { useCan, useHasRole } from '@/features/auth/useCan'
import { enumLabel } from '@/lib/enumLabels'
import { toNum, formatDateTime } from '@/lib/utils'
import { useDebouncedValue } from '@/lib/useDebouncedValue'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { Pagination } from '@/components/shared/Pagination'
import { CreateMaterialDialog } from './CreateMaterialDialog'
import { MaterialDetailDialog } from './MaterialDetailDialog'

const TYPES: MaterialType[] = ['Video', 'Article', 'Link']

const TYPE_ICONS: Record<MaterialType, typeof Video> = {
  Video: Video,
  Article: FileText,
  Link: LinkIcon,
}

export function MaterialsListPage() {
  const { t, i18n } = useTranslation()
  const lang = i18n.language === 'en' ? 'en' : 'uk'
  const queryClient = useQueryClient()
  const canManage = useCan('CanManageMaterials')
  const isTeacher = useHasRole('Teacher')
  const canCreate = canManage || isTeacher

  const [searchParams, setSearchParams] = useSearchParams()
  const [searchInput, setSearchInput] = useState(searchParams.get('search') ?? '')
  const debouncedSearch = useDebouncedValue(searchInput)

  const page = Number(searchParams.get('page') ?? '1')
  const courseId = searchParams.get('courseId') ?? ''
  const type = searchParams.get('type') ?? ''

  const [createOpen, setCreateOpen] = useState(false)
  const [selected, setSelected] = useState<MaterialListItem | null>(null)

  const { data: courses } = useQuery({
    queryKey: ['courses', 'lookup-all'],
    queryFn: getAllCoursesForLookup,
    staleTime: 5 * 60_000,
  })
  const courseNameById = new Map((courses ?? []).map((c) => [c.id, c.name]))

  const filters = useMemo(
    () => ({
      search: debouncedSearch || undefined,
      courseId: courseId || undefined,
      type: (type || undefined) as MaterialType | undefined,
      page,
      pageSize: 20,
    }),
    [debouncedSearch, courseId, type, page],
  )

  const { data, isPending, isPlaceholderData } = useQuery({
    queryKey: ['materials', filters],
    queryFn: () => getMaterials(filters),
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

  function invalidate() {
    queryClient.invalidateQueries({ queryKey: ['materials'] })
  }

  const hasFilters = !!(courseId || type || debouncedSearch)

  return (
    <div className="flex flex-col gap-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-2xl font-semibold tracking-tight">{t('materials.title')}</h1>
        {canCreate && (
          <Button onClick={() => setCreateOpen(true)}>
            <Plus />
            {t('materials.add')}
          </Button>
        )}
      </div>

      <div className="flex flex-wrap items-end gap-3">
        <div className="min-w-56 flex-1">
          <Input
            placeholder={t('materials.searchPlaceholder')}
            value={searchInput}
            onChange={(e) => {
              setSearchInput(e.target.value)
              updateParam('search', e.target.value)
            }}
          />
        </div>
        <Select
          value={courseId}
          onValueChange={(v) => updateParam('courseId', v === 'any' ? '' : v)}
        >
          <SelectTrigger className="w-48">
            <SelectValue placeholder={t('materials.filters.course')} />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="any">{t('materials.filters.any')}</SelectItem>
            {courses?.map((c) => (
              <SelectItem key={c.id} value={c.id}>
                {c.name}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        <Select value={type} onValueChange={(v) => updateParam('type', v === 'any' ? '' : v)}>
          <SelectTrigger className="w-40">
            <SelectValue placeholder={t('materials.filters.type')} />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="any">{t('materials.filters.any')}</SelectItem>
            {TYPES.map((tp) => (
              <SelectItem key={tp} value={tp}>
                {enumLabel(t, 'materialType', tp)}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        {hasFilters && (
          <Button
            variant="ghost"
            onClick={() => {
              setSearchInput('')
              setSearchParams({}, { replace: true })
            }}
          >
            {t('materials.filters.clear')}
          </Button>
        )}
      </div>

      {isPending && (
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
          <Skeleton className="h-32 w-full" />
          <Skeleton className="h-32 w-full" />
          <Skeleton className="h-32 w-full" />
        </div>
      )}

      {!isPending && (data?.items.length ?? 0) === 0 && (
        <p className="text-sm text-muted-foreground">{t('materials.empty')}</p>
      )}

      <div
        className={
          'grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3' +
          (isPlaceholderData ? ' opacity-60 transition-opacity' : '')
        }
      >
        {data?.items.map((m) => {
          const Icon = TYPE_ICONS[m.type]
          return (
            <Card
              key={m.id}
              className="cursor-pointer hover:bg-muted/40"
              onClick={() => setSelected(m)}
            >
              <CardHeader className="flex flex-row items-start justify-between gap-2">
                <CardTitle className="text-base font-medium leading-snug">{m.title}</CardTitle>
                <Badge variant="outline" className="shrink-0 gap-1">
                  <Icon className="size-3" />
                  {enumLabel(t, 'materialType', m.type)}
                </Badge>
              </CardHeader>
              <CardContent className="flex flex-col gap-1">
                <span className="text-sm text-muted-foreground">
                  {courseNameById.get(m.courseId) ?? '—'}
                </span>
                {m.description && (
                  <p className="line-clamp-2 text-sm text-muted-foreground">{m.description}</p>
                )}
                <span className="text-xs text-muted-foreground">
                  {formatDateTime(m.createdAt, lang)}
                </span>
              </CardContent>
            </Card>
          )
        })}
      </div>

      {data && (
        <Pagination
          page={toNum(data.page)}
          totalPages={toNum(data.totalPages)}
          totalCount={toNum(data.totalCount)}
          onPageChange={goToPage}
        />
      )}

      <CreateMaterialDialog open={createOpen} onOpenChange={setCreateOpen} onCreated={invalidate} />
      <MaterialDetailDialog
        open={!!selected}
        onOpenChange={(open) => !open && setSelected(null)}
        courseName={selected ? courseNameById.get(selected.courseId) : undefined}
        row={selected}
        onChanged={invalidate}
      />
    </div>
  )
}
