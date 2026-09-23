import type { ReactNode } from 'react'
import { Navigate } from 'react-router-dom'
import { useAuth } from '@/features/auth/useAuth'
import type { Permission, Role } from '@/lib/permissions'

interface RequireAccessProps {
  /** User needs this permission... */
  permission?: Permission
  /** ...OR one of these roles (own-data screens for Teacher/Student). */
  roles?: Role[]
  children: ReactNode
}

/** Client-side convenience gate; the server is the real authority and still
 * answers 403 for anything this misses. */
export function RequireAccess({ permission, roles, children }: RequireAccessProps) {
  const { user } = useAuth()
  const hasPermission = permission ? (user?.permissions?.includes(permission) ?? false) : false
  const hasRole = roles ? roles.some((r) => user?.roles?.includes(r)) : false
  const allowed = (!permission && !roles) || hasPermission || hasRole
  if (!allowed) return <Navigate to="/403" replace />
  return <>{children}</>
}
