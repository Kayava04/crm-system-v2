import type { ReactNode } from 'react'
import { Navigate, useLocation } from 'react-router-dom'
import { useAuth } from '@/features/auth/useAuth'
import { FullPageSpinner } from '@/components/layout/FullPageSpinner'

export function RequireAuth({
  children,
  allowMustChange = false,
}: {
  children: ReactNode
  allowMustChange?: boolean
}) {
  const { status, user } = useAuth()
  const location = useLocation()

  if (status === 'loading') return <FullPageSpinner />
  if (status === 'unauthenticated') {
    return <Navigate to="/login" replace state={{ from: location }} />
  }
  if (user?.mustChangePassword && !allowMustChange) {
    return <Navigate to="/change-password" replace />
  }
  if (!user?.mustChangePassword && allowMustChange) {
    return <Navigate to="/" replace />
  }
  return <>{children}</>
}
