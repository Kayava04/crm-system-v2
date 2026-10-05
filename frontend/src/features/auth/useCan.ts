import { useAuth } from './useAuth'
import type { Permission, Role } from '@/lib/permissions'

export function useCan(permission: Permission): boolean {
  const { user } = useAuth()
  return user?.permissions?.includes(permission) ?? false
}

export function useCanAny(permissions: Permission[]): boolean {
  const { user } = useAuth()
  if (!user) return false
  return permissions.some((p) => user.permissions?.includes(p))
}

export function useHasRole(role: Role): boolean {
  const { user } = useAuth()
  return user?.roles?.includes(role) ?? false
}

export function useIsStaff(): boolean {
  const isAdmin = useHasRole('Admin')
  const isSuperAdmin = useHasRole('SuperAdmin')
  return isAdmin || isSuperAdmin
}
