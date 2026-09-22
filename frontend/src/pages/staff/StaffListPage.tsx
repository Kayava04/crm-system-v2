import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { Plus } from 'lucide-react'
import { getStaff } from '@/features/staff/api'
import type { StaffMember } from '@/features/staff/api'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Input } from '@/components/ui/input'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { DataTable, type DataTableColumn } from '@/components/shared/DataTable'
import { CreateStaffDialog } from './CreateStaffDialog'
import { StaffDetailDialog } from './StaffDetailDialog'
import { TemporaryPasswordDialog } from '@/components/shared/TemporaryPasswordDialog'

export function StaffListPage() {
  const { t } = useTranslation()
  const queryClient = useQueryClient()

  const [search, setSearch] = useState('')
  const [statusFilter, setStatusFilter] = useState('')
  const [createOpen, setCreateOpen] = useState(false)
  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [createdResult, setCreatedResult] = useState<{
    email: string
    temporaryPassword: string
  } | null>(null)

  const isActive =
    statusFilter === 'active' ? true : statusFilter === 'inactive' ? false : undefined

  const { data, isPending } = useQuery({
    queryKey: ['staff', isActive],
    queryFn: () => getStaff(isActive),
  })

  const selected = data?.find((m) => m.id === selectedId) ?? null

  // GET /api/auth/users only filters by isActive server-side, so name/email/
  // phone search is done client-side over the already-fetched (short) staff list.
  const filteredStaff = (data ?? []).filter((m) => {
    if (!search) return true
    const term = search.toLowerCase()
    const haystack = [m.fullName ?? '', m.email, m.phoneNumber ?? ''].join(' ').toLowerCase()
    return haystack.includes(term)
  })

  const hasFilters = !!(search || statusFilter)

  function invalidate() {
    queryClient.invalidateQueries({ queryKey: ['staff'] })
  }

  const columns: DataTableColumn<StaffMember>[] = [
    {
      key: 'name',
      header: t('staff.columns.name'),
      cell: (row) => <span className="font-medium">{row.fullName ?? row.email}</span>,
    },
    {
      key: 'email',
      header: t('staff.columns.email'),
      cell: (row) => row.email,
    },
    {
      key: 'phone',
      header: t('staff.columns.phone'),
      cell: (row) => row.phoneNumber ?? '—',
    },
    {
      key: 'permissions',
      header: t('staff.columns.permissions'),
      cell: (row) => row.permissions.length,
    },
    {
      key: 'status',
      header: t('staff.columns.status'),
      cell: (row) => (
        <Badge variant={row.isActive ? 'success' : 'secondary'}>
          {row.isActive ? t('staff.active') : t('staff.inactive')}
        </Badge>
      ),
    },
  ]

  return (
    <div className="flex flex-col gap-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-2xl font-semibold tracking-tight">{t('staff.title')}</h1>
        <Button onClick={() => setCreateOpen(true)}>
          <Plus />
          {t('staff.add')}
        </Button>
      </div>

      <div className="flex flex-wrap items-end gap-3">
        <div className="min-w-56 flex-1">
          <Input
            placeholder={t('staff.searchPlaceholder')}
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
        </div>
        <Select value={statusFilter} onValueChange={(v) => setStatusFilter(v === 'any' ? '' : v)}>
          <SelectTrigger className="w-40">
            <SelectValue placeholder={t('staff.filters.status')} />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="any">{t('staff.filters.any')}</SelectItem>
            <SelectItem value="active">{t('staff.filters.active')}</SelectItem>
            <SelectItem value="inactive">{t('staff.filters.inactive')}</SelectItem>
          </SelectContent>
        </Select>
        {hasFilters && (
          <Button
            variant="ghost"
            onClick={() => {
              setSearch('')
              setStatusFilter('')
            }}
          >
            {t('staff.filters.clear')}
          </Button>
        )}
      </div>

      <DataTable
        columns={columns}
        rows={filteredStaff}
        rowKey={(row) => row.id}
        isLoading={isPending}
        emptyMessage={t('staff.empty')}
        onRowClick={(row) => setSelectedId(row.id)}
      />

      <CreateStaffDialog
        open={createOpen}
        onOpenChange={setCreateOpen}
        onCreated={(result) => {
          invalidate()
          setCreatedResult(result)
        }}
      />
      <StaffDetailDialog
        open={!!selectedId}
        onOpenChange={(open) => !open && setSelectedId(null)}
        member={selected}
        onChanged={invalidate}
      />
      <TemporaryPasswordDialog
        open={!!createdResult}
        onOpenChange={(open) => !open && setCreatedResult(null)}
        email={createdResult?.email}
        password={createdResult?.temporaryPassword ?? null}
      />
    </div>
  )
}
