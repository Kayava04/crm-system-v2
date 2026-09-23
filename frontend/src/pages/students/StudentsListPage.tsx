import { useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { useQuery, useQueryClient, keepPreviousData } from '@tanstack/react-query'
import { toast } from 'sonner'
import { Plus, FileUp } from 'lucide-react'
import {
  getStudents,
  bulkDeleteStudents,
  getStudentExportUrl,
  getStudentImportTemplateUrl,
  importStudents,
} from '@/features/students/api'
import type { StudentListItem } from '@/features/students/api'
import { ApiError } from '@/api/errors'
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
import { toNum } from '@/lib/utils'
import { useCan } from '@/features/auth/useCan'

const LANGUAGES = ['English', 'French', 'German', 'Polish', 'Spanish', 'Italian'] as const
const LEVELS = ['A1', 'A2', 'B1', 'B2', 'C1', 'C2'] as const
const FORMATS = ['Online', 'Offline'] as const

export function StudentsListPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const canManage = useCan('CanManageStudents')
  const canDelete = useCan('CanDeleteStudents')
  const canCreate = useCan('CanCreateStudents')

  const [searchParams, setSearchParams] = useSearchParams()
  const [searchInput, setSearchInput] = useState(searchParams.get('search') ?? '')
  const debouncedSearch = useDebouncedValue(searchInput)
  const [selected, setSelected] = useState<Set<string>>(new Set())
  const [bulkDeleteOpen, setBulkDeleteOpen] = useState(false)
  const [importOpen, setImportOpen] = useState(false)

  const page = Number(searchParams.get('page') ?? '1')
  const city = searchParams.get('city') ?? ''
  const isChild = searchParams.get('isChild') ?? ''
  const language = searchParams.get('language') ?? ''
  const currentLevel = searchParams.get('currentLevel') ?? ''
  const format = searchParams.get('format') ?? ''

  const filters = useMemo(
    () => ({
      search: debouncedSearch || undefined,
      city: city || undefined,
      isChild: isChild === '' ? undefined : isChild === 'true',
      language: (language || undefined) as never,
      currentLevel: (currentLevel || undefined) as never,
      format: (format || undefined) as never,
      page,
      pageSize: 20,
    }),
    [debouncedSearch, city, isChild, language, currentLevel, format, page],
  )

  const { data, isPending, isPlaceholderData } = useQuery({
    queryKey: ['students', filters],
    queryFn: () => getStudents(filters),
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

  const hasFilters = !!(city || isChild || language || currentLevel || format || debouncedSearch)

  const columns: DataTableColumn<StudentListItem>[] = [
    ...(canDelete
      ? [
          {
            key: 'select',
            header: '',
            width: '2.5rem',
            cell: (row: StudentListItem) => (
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
      header: t('students.columns.name'),
      cell: (row) => <span className="font-medium">{row.fullName}</span>,
    },
    { key: 'email', header: t('students.columns.email'), cell: (row) => row.email },
    { key: 'phone', header: t('students.columns.phone'), cell: (row) => row.phoneNumber },
    {
      key: 'child',
      header: t('students.columns.child'),
      cell: (row) =>
        row.isChild ? <Badge variant="secondary">{t('students.filters.yes')}</Badge> : '—',
    },
  ]

  async function handleBulkDelete() {
    try {
      const result = await bulkDeleteStudents(Array.from(selected))
      toast.success(
        t('table.bulkDeleteResult', { succeeded: result.succeeded, total: result.total }),
      )
      setSelected(new Set())
      await queryClient.invalidateQueries({ queryKey: ['students'] })
    } catch (err) {
      toast.error(
        err instanceof ApiError ? err.detail || t('common.unknownError') : t('common.networkError'),
      )
    }
  }

  return (
    <div className="flex flex-col gap-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-2xl font-semibold tracking-tight">{t('students.title')}</h1>
        <div className="flex flex-wrap items-center gap-2">
          <ExportMenu
            buildUrl={(fmt, lang) => getStudentExportUrl(filters, fmt, lang)}
            filenamePrefix="students"
          />
          {canManage && (
            <Button variant="outline" onClick={() => setImportOpen(true)}>
              <FileUp />
              {t('import.title')}
            </Button>
          )}
          {canCreate && (
            <Button onClick={() => navigate('/students/new')}>
              <Plus />
              {t('students.add')}
            </Button>
          )}
        </div>
      </div>

      <div className="flex flex-wrap items-end gap-3">
        <div className="flex min-w-56 flex-1 flex-col gap-1.5">
          <Input
            placeholder={t('students.searchPlaceholder')}
            value={searchInput}
            onChange={(e) => {
              setSearchInput(e.target.value)
              updateParam('search', e.target.value)
            }}
          />
        </div>
        <div className="flex w-36 flex-col gap-1.5">
          <Input
            placeholder={t('students.filters.city')}
            value={city}
            onChange={(e) => updateParam('city', e.target.value)}
          />
        </div>
        <Select value={isChild} onValueChange={(v) => updateParam('isChild', v === 'any' ? '' : v)}>
          <SelectTrigger className="w-36">
            <SelectValue placeholder={t('students.filters.isChild')} />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="any">{t('students.filters.any')}</SelectItem>
            <SelectItem value="true">{t('students.filters.yes')}</SelectItem>
            <SelectItem value="false">{t('students.filters.no')}</SelectItem>
          </SelectContent>
        </Select>
        <Select
          value={language}
          onValueChange={(v) => updateParam('language', v === 'any' ? '' : v)}
        >
          <SelectTrigger className="w-36">
            <SelectValue placeholder={t('students.filters.language')} />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="any">{t('students.filters.any')}</SelectItem>
            {LANGUAGES.map((l) => (
              <SelectItem key={l} value={l}>
                {t(`enums.language.${l}`)}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        <Select
          value={currentLevel}
          onValueChange={(v) => updateParam('currentLevel', v === 'any' ? '' : v)}
        >
          <SelectTrigger className="w-28">
            <SelectValue placeholder={t('students.filters.level')} />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="any">{t('students.filters.any')}</SelectItem>
            {LEVELS.map((l) => (
              <SelectItem key={l} value={l}>
                {l}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        <Select value={format} onValueChange={(v) => updateParam('format', v === 'any' ? '' : v)}>
          <SelectTrigger className="w-32">
            <SelectValue placeholder={t('students.filters.format')} />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="any">{t('students.filters.any')}</SelectItem>
            {FORMATS.map((f) => (
              <SelectItem key={f} value={f}>
                {t(`enums.format.${f}`)}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        {hasFilters && (
          <Button variant="ghost" onClick={clearFilters}>
            {t('students.filters.clear')}
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
          onRowClick={(row) => navigate(`/students/${row.id}`)}
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
        templateUrl={(fmt, lang) => getStudentImportTemplateUrl(fmt, lang)}
        importFn={importStudents}
        onImported={() => queryClient.invalidateQueries({ queryKey: ['students'] })}
      />
    </div>
  )
}
