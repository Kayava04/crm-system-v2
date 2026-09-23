import { useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { useQuery, useQueryClient, keepPreviousData } from '@tanstack/react-query'
import { toast } from 'sonner'
import { Plus, FileUp } from 'lucide-react'
import {
  getTeachers,
  bulkDeleteTeachers,
  getTeacherExportUrl,
  getTeacherImportTemplateUrl,
  importTeachers,
} from '@/features/teachers/api'
import type { TeacherListItem } from '@/features/teachers/api'
import { ApiError } from '@/api/errors'
import { enumLabel } from '@/lib/enumLabels'
import { toNum } from '@/lib/utils'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Checkbox } from '@/components/ui/checkbox'
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
import { ConfirmDialog } from '@/components/shared/ConfirmDialog'
import { ExportMenu } from '@/components/shared/ExportMenu'
import { ImportWizardDialog } from '@/components/shared/ImportWizardDialog'
import { useDebouncedValue } from '@/lib/useDebouncedValue'
import { useCan } from '@/features/auth/useCan'

const STATUSES = ['Probation', 'Employed', 'OnLeave', 'Resigned', 'Dismissed'] as const

function statusVariant(status: string): 'success' | 'warning' | 'secondary' | 'destructive' {
  if (status === 'Employed' || status === 'Probation') return 'success'
  if (status === 'OnLeave') return 'warning'
  if (status === 'Resigned') return 'secondary'
  return 'destructive'
}

export function TeachersListPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const canManage = useCan('CanManageTeachers')
  const canDelete = useCan('CanDeleteTeachers')
  const canCreate = useCan('CanCreateTeachers')

  const [searchParams, setSearchParams] = useSearchParams()
  const [searchInput, setSearchInput] = useState(searchParams.get('search') ?? '')
  const debouncedSearch = useDebouncedValue(searchInput)
  const [selected, setSelected] = useState<Set<string>>(new Set())
  const [bulkDeleteOpen, setBulkDeleteOpen] = useState(false)
  const [importOpen, setImportOpen] = useState(false)

  const page = Number(searchParams.get('page') ?? '1')
  const city = searchParams.get('city') ?? ''
  const status = searchParams.get('status') ?? ''

  const filters = useMemo(
    () => ({
      search: debouncedSearch || undefined,
      city: city || undefined,
      status: (status || undefined) as never,
      page,
      pageSize: 20,
    }),
    [debouncedSearch, city, status, page],
  )

  const { data, isPending, isPlaceholderData } = useQuery({
    queryKey: ['teachers', filters],
    queryFn: () => getTeachers(filters),
    placeholderData: keepPreviousData,
  })

  function updateParam(key: string, value: string) {
    const next = new URLSearchParams(searchParams)
    if (value) next.set(key, value)
    else next.delete(key)
    next.delete('page')
    setSearchParams(next, { replace: true })
    setSelected(new Set())
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

  const hasFilters = !!(city || status || debouncedSearch)

  const columns: DataTableColumn<TeacherListItem>[] = [
    ...(canDelete
      ? [
          {
            key: 'select',
            header: '',
            width: '2.5rem',
            cell: (row: TeacherListItem) => (
              <Checkbox
                checked={selected.has(row.id)}
                onCheckedChange={(checked) => {
                  setSelected((prev) => {
                    const next = new Set(prev)
                    if (checked) next.add(row.id)
                    else next.delete(row.id)
                    return next
                  })
                }}
                onClick={(e) => e.stopPropagation()}
                aria-label={row.fullName}
              />
            ),
          },
        ]
      : []),
    {
      key: 'name',
      header: t('teachers.columns.name'),
      cell: (row) => <span className="font-medium">{row.fullName}</span>,
    },
    { key: 'email', header: t('teachers.columns.email'), cell: (row) => row.email },
    { key: 'phone', header: t('teachers.columns.phone'), cell: (row) => row.phoneNumber },
    {
      key: 'status',
      header: t('teachers.columns.status'),
      cell: (row) => (
        <Badge variant={statusVariant(row.status)}>
          {enumLabel(t, 'teacherStatus', row.status)}
        </Badge>
      ),
    },
  ]

  async function handleBulkDelete() {
    try {
      const result = await bulkDeleteTeachers(Array.from(selected))
      toast.success(
        t('table.bulkDeleteResult', { succeeded: result.succeeded, total: result.total }),
      )
      setSelected(new Set())
      await queryClient.invalidateQueries({ queryKey: ['teachers'] })
    } catch (err) {
      toast.error(
        err instanceof ApiError ? err.detail || t('common.unknownError') : t('common.networkError'),
      )
    }
  }

  return (
    <div className="flex flex-col gap-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-2xl font-semibold tracking-tight">{t('teachers.title')}</h1>
        <div className="flex flex-wrap items-center gap-2">
          <ExportMenu
            buildUrl={(fmt, lang) => getTeacherExportUrl(filters, fmt, lang)}
            filenamePrefix="teachers"
          />
          {canManage && (
            <Button variant="outline" onClick={() => setImportOpen(true)}>
              <FileUp />
              {t('import.title')}
            </Button>
          )}
          {canCreate && (
            <Button onClick={() => navigate('/teachers/new')}>
              <Plus />
              {t('teachers.add')}
            </Button>
          )}
        </div>
      </div>

      <div className="flex flex-wrap items-end gap-3">
        <div className="flex min-w-56 flex-1 flex-col gap-1.5">
          <Input
            placeholder={t('teachers.searchPlaceholder')}
            value={searchInput}
            onChange={(e) => {
              setSearchInput(e.target.value)
              updateParam('search', e.target.value)
            }}
          />
        </div>
        <div className="flex w-36 flex-col gap-1.5">
          <Input
            placeholder={t('teachers.filters.city')}
            value={city}
            onChange={(e) => updateParam('city', e.target.value)}
          />
        </div>
        <Select value={status} onValueChange={(v) => updateParam('status', v === 'any' ? '' : v)}>
          <SelectTrigger className="w-44">
            <SelectValue placeholder={t('teachers.filters.status')} />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="any">{t('teachers.filters.any')}</SelectItem>
            {STATUSES.map((s) => (
              <SelectItem key={s} value={s}>
                {enumLabel(t, 'teacherStatus', s)}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        {hasFilters && (
          <Button variant="ghost" onClick={clearFilters}>
            {t('teachers.filters.clear')}
          </Button>
        )}
      </div>

      {selected.size > 0 && (
        <div className="flex items-center gap-3 rounded-md border border-border bg-muted/40 px-3 py-2">
          <span className="text-sm">{t('table.selected', { count: selected.size })}</span>
          <Button variant="ghost" size="sm" onClick={() => setSelected(new Set())}>
            {t('table.clearSelection')}
          </Button>
          <Button variant="destructive" size="sm" onClick={() => setBulkDeleteOpen(true)}>
            {t('common.delete')}
          </Button>
        </div>
      )}

      <div className={isPlaceholderData ? 'opacity-60 transition-opacity' : undefined}>
        <DataTable
          columns={columns}
          rows={data?.items ?? []}
          rowKey={(row) => row.id}
          isLoading={isPending}
          onRowClick={(row) => navigate(`/teachers/${row.id}`)}
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

      <ConfirmDialog
        open={bulkDeleteOpen}
        onOpenChange={setBulkDeleteOpen}
        title={t('table.bulkDeleteConfirmTitle')}
        description={t('table.bulkDeleteConfirmDesc')}
        destructive
        onConfirm={handleBulkDelete}
      />

      <ImportWizardDialog
        open={importOpen}
        onOpenChange={setImportOpen}
        title={t('import.title')}
        templateUrl={(fmt, lang) => getTeacherImportTemplateUrl(fmt, lang)}
        importFn={importTeachers}
        onImported={() => queryClient.invalidateQueries({ queryKey: ['teachers'] })}
      />
    </div>
  )
}
