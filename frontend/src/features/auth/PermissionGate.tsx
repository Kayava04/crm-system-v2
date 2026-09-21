import type { ReactNode } from 'react'
import { useCan } from './useCan'
import type { Permission } from '@/lib/permissions'

/** Renders children only if the user holds `permission`. Purely cosmetic — the
 * server remains the authority and every endpoint is checked there too. */
export function PermissionGate({
  permission,
  children,
}: {
  permission: Permission
  children: ReactNode
}) {
  const can = useCan(permission)
  return can ? <>{children}</> : null
}
