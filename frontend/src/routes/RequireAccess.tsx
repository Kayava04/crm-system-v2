import type { ReactNode } from 'react'
import { Navigate } from 'react-router-dom'
import { useAuth } from '@/features/auth/useAuth'
import type { Permission, Role } from '@/lib/permissions'

interface RequireAccessProps {
  permission?: Permission
  roles?: Role[]
  children: ReactNode
}

export function RequireAccess({ permission, roles, children }: RequireAccessProps) {
  const { user } = useAuth()
  const hasPermission = permission ? (user?.permissions?.includes(permission) ?? false) : false
  const hasRole = roles ? roles.some((r) => user?.roles?.includes(r)) : false
  const allowed = (!permission && !roles) || hasPermission || hasRole
  if (!allowed) return <Navigate to="/403" replace />
  return <>{children}</>
}
