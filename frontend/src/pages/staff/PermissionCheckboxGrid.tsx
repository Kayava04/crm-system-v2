import { useTranslation } from 'react-i18next'
import type { PermissionOption } from '@/features/staff/api'
import { PERMISSIONS } from '@/lib/permissions'
import { Checkbox } from '@/components/ui/checkbox'
import { Label } from '@/components/ui/label'

interface PermissionCheckboxGridProps {
  options: PermissionOption[]
  selectedIds: Set<string>
  onChange: (next: Set<string>) => void
}

/** Renders the 21 backend permissions as checkboxes, ordered by the frontend's
 * own PERMISSIONS constant (stable, familiar order) rather than however the
 * backend happens to return them, matched to the backend's permission ids by name. */
export function PermissionCheckboxGrid({
  options,
  selectedIds,
  onChange,
}: PermissionCheckboxGridProps) {
  const { t } = useTranslation()
  const optionByName = new Map(options.map((o) => [o.name, o]))
  const availableIds = PERMISSIONS.map((name) => optionByName.get(name)?.id).filter(
    (id): id is string => !!id,
  )
  const allSelected = availableIds.length > 0 && availableIds.every((id) => selectedIds.has(id))

  function toggle(id: string, checked: boolean) {
    const next = new Set(selectedIds)
    if (checked) next.add(id)
    else next.delete(id)
    onChange(next)
  }

  function toggleAll(checked: boolean) {
    onChange(checked ? new Set(availableIds) : new Set())
  }

  return (
    <div className="flex flex-col gap-3">
      <div className="flex items-center gap-2 border-b border-border pb-2">
        <Checkbox
          id="perm-select-all"
          checked={allSelected}
          onCheckedChange={(v) => toggleAll(v === true)}
        />
        <Label htmlFor="perm-select-all" className="font-normal text-muted-foreground">
          {allSelected ? t('permissions.deselectAll') : t('permissions.selectAll')}
        </Label>
      </div>
      <div className="grid grid-cols-1 gap-2 sm:grid-cols-2">
        {PERMISSIONS.map((name) => {
          const option = optionByName.get(name)
          if (!option) return null
          const checked = selectedIds.has(option.id)
          return (
            <div key={option.id} className="flex items-center gap-2">
              <Checkbox
                id={`perm-${option.id}`}
                checked={checked}
                onCheckedChange={(v) => toggle(option.id, v === true)}
              />
              <Label htmlFor={`perm-${option.id}`} className="font-normal">
                {t(`permissions.${name}`)}
              </Label>
            </div>
          )
        })}
      </div>
    </div>
  )
}
