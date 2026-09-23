import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useQuery } from '@tanstack/react-query'
import { toast } from 'sonner'
import { KeyRound, UserCheck, UserX } from 'lucide-react'
import {
  getPermissionOptions,
  setStaffPermissions,
  setStaffStatus,
  setStaffSalary,
  resetStaffPassword,
} from '@/features/staff/api'
import type { PermissionOption, StaffMember } from '@/features/staff/api'
import { ApiError } from '@/api/errors'
import { formatDate, formatCurrency } from '@/lib/utils'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { ConfirmDialog } from '@/components/shared/ConfirmDialog'
import { PermissionCheckboxGrid } from './PermissionCheckboxGrid'
import { TemporaryPasswordDialog } from '@/components/shared/TemporaryPasswordDialog'

interface StaffDetailDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  member: StaffMember | null
  onChanged: () => void
}

export function StaffDetailDialog({
  open,
  onOpenChange,
  member,
  onChanged,
}: StaffDetailDialogProps) {
  const { t, i18n } = useTranslation()
  const lang = i18n.language === 'en' ? 'en' : 'uk'

  const [statusDialogOpen, setStatusDialogOpen] = useState(false)
  const [resetOpen, setResetOpen] = useState(false)
  const [tempPassword, setTempPassword] = useState<string | null>(null)

  const { data: options } = useQuery({
    queryKey: ['staff', 'permission-options'],
    queryFn: getPermissionOptions,
    enabled: open,
    staleTime: 5 * 60_000,
  })

  if (!member) return null

  async function handleToggleStatus() {
    await setStaffStatus(member!.id, !member!.isActive)
    toast.success(t('staff.detail.statusChanged'))
    onChanged()
  }

  async function handleResetPassword() {
    const result = await resetStaffPassword(member!.id)
    setTempPassword(result.temporaryPassword)
  }

  return (
    <>
      <Dialog open={open} onOpenChange={onOpenChange}>
        <DialogContent className="max-h-[85vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              {member.fullName ?? member.email}
              <Badge variant={member.isActive ? 'success' : 'secondary'}>
                {member.isActive ? t('staff.active') : t('staff.inactive')}
              </Badge>
            </DialogTitle>
          </DialogHeader>

          <div className="flex flex-col gap-1 text-sm">
            <div className="flex justify-between">
              <span className="text-muted-foreground">{t('staff.columns.email')}</span>
              <span>{member.email}</span>
            </div>
            {member.phoneNumber && (
              <div className="flex justify-between">
                <span className="text-muted-foreground">{t('staff.columns.phone')}</span>
                <span>{member.phoneNumber}</span>
              </div>
            )}
            {member.middleName && (
              <div className="flex justify-between">
                <span className="text-muted-foreground">{t('staff.detail.middleName')}</span>
                <span>{member.middleName}</span>
              </div>
            )}
            {member.dateOfBirth && (
              <div className="flex justify-between">
                <span className="text-muted-foreground">{t('staff.detail.dateOfBirth')}</span>
                <span>{formatDate(member.dateOfBirth, lang)}</span>
              </div>
            )}
            {member.city && (
              <div className="flex justify-between">
                <span className="text-muted-foreground">{t('staff.detail.city')}</span>
                <span>{member.city}</span>
              </div>
            )}
            {member.country && (
              <div className="flex justify-between">
                <span className="text-muted-foreground">{t('staff.detail.country')}</span>
                <span>{member.country}</span>
              </div>
            )}
            <div className="flex justify-between">
              <span className="text-muted-foreground">{t('staff.detail.createdAt')}</span>
              <span>{formatDate(member.createdAt, lang)}</span>
            </div>
          </div>

          <SalaryEditor key={`${member.id}-salary`} member={member} onChanged={onChanged} />

          {options && (
            <PermissionsEditor
              key={member.id}
              member={member}
              options={options}
              onChanged={onChanged}
            />
          )}

          <DialogFooter className="flex-wrap gap-2">
            <Button variant="outline" onClick={() => setResetOpen(true)}>
              <KeyRound />
              {t('staff.detail.resetPassword')}
            </Button>
            <Button
              variant={member.isActive ? 'destructive' : 'default'}
              onClick={() => setStatusDialogOpen(true)}
            >
              {member.isActive ? <UserX /> : <UserCheck />}
              {member.isActive ? t('staff.detail.deactivate') : t('staff.detail.reactivate')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <ConfirmDialog
        open={statusDialogOpen}
        onOpenChange={setStatusDialogOpen}
        title={
          member.isActive
            ? t('staff.detail.confirmDeactivateTitle')
            : t('staff.detail.confirmReactivateTitle')
        }
        description={
          member.isActive
            ? t('staff.detail.confirmDeactivateDesc')
            : t('staff.detail.confirmReactivateDesc')
        }
        destructive={member.isActive}
        onConfirm={handleToggleStatus}
      />

      <ConfirmDialog
        open={resetOpen}
        onOpenChange={setResetOpen}
        title={t('staff.detail.confirmResetTitle')}
        description={t('staff.detail.confirmResetDesc')}
        onConfirm={handleResetPassword}
      />

      <TemporaryPasswordDialog
        open={!!tempPassword}
        onOpenChange={(next) => !next && setTempPassword(null)}
        email={member.email}
        password={tempPassword}
      />
    </>
  )
}

/** Set or clear an administrator's salary — CanManageAdmins-only and never
 * self-service (the caller cannot open this dialog for their own account to
 * begin with: StaffListPage never lists the signed-in user). Keyed by
 * member.id from the parent, same reasoning as PermissionsEditor below. */
function SalaryEditor({ member, onChanged }: { member: StaffMember; onChanged: () => void }) {
  const { t, i18n } = useTranslation()
  const lang = i18n.language === 'en' ? 'en' : 'uk'
  const [value, setValue] = useState(member.salary != null ? String(member.salary) : '')
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const parsed = value.trim() === '' ? null : Number(value)
  const invalid = value.trim() !== '' && (Number.isNaN(parsed) || (parsed as number) < 0)

  async function save(next: number | null) {
    setError(null)
    setSaving(true)
    try {
      await setStaffSalary(member.id, next)
      setValue(next != null ? String(next) : '')
      toast.success(t('staff.detail.salarySaved'))
      onChanged()
    } catch (err) {
      toast.error(
        err instanceof ApiError ? err.detail || t('common.unknownError') : t('common.networkError'),
      )
    } finally {
      setSaving(false)
    }
  }

  function handleSave() {
    if (invalid) {
      setError(t('staff.detail.salaryInvalid'))
      return
    }
    void save(parsed)
  }

  return (
    <div className="flex flex-col gap-2">
      <p className="text-sm font-medium">{t('staff.detail.salaryTitle')}</p>
      {member.salary != null && (
        <p className="text-sm text-muted-foreground">{formatCurrency(member.salary, lang)}</p>
      )}
      <div className="flex items-end gap-2">
        <div className="flex flex-1 flex-col gap-1.5">
          <Input
            type="number"
            min={0}
            step="0.01"
            placeholder={t('staff.detail.salaryPlaceholder')}
            value={value}
            onChange={(e) => setValue(e.target.value)}
            aria-invalid={invalid}
          />
          {error && <p className="text-xs text-destructive">{error}</p>}
        </div>
        <Button size="sm" loading={saving} onClick={handleSave}>
          {t('staff.detail.salarySave')}
        </Button>
        {member.salary != null && (
          <Button size="sm" variant="outline" loading={saving} onClick={() => void save(null)}>
            {t('staff.detail.salaryClear')}
          </Button>
        )}
      </div>
    </div>
  )
}

/** Keyed by member.id from the parent so a fresh instance (and fresh local
 * selection state) mounts whenever the selected staff member changes — avoids
 * syncing props into state via an effect. */
function PermissionsEditor({
  member,
  options,
  onChanged,
}: {
  member: StaffMember
  options: PermissionOption[]
  onChanged: () => void
}) {
  const { t } = useTranslation()
  const idsByName = new Map(options.map((o) => [o.name, o.id]))
  const [selectedIds, setSelectedIds] = useState(
    () =>
      new Set(
        member.permissions.map((name) => idsByName.get(name)).filter((id): id is string => !!id),
      ),
  )
  const [saving, setSaving] = useState(false)

  async function handleSave() {
    setSaving(true)
    try {
      await setStaffPermissions(member.id, Array.from(selectedIds))
      toast.success(t('staff.detail.saved'))
      onChanged()
    } catch (err) {
      toast.error(
        err instanceof ApiError ? err.detail || t('common.unknownError') : t('common.networkError'),
      )
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="flex flex-col gap-2">
      <p className="text-sm font-medium">{t('staff.detail.permissionsTitle')}</p>
      <PermissionCheckboxGrid
        options={options}
        selectedIds={selectedIds}
        onChange={setSelectedIds}
      />
      <Button size="sm" loading={saving} className="w-fit" onClick={handleSave}>
        {t('staff.detail.save')}
      </Button>
    </div>
  )
}
