import type { ReactNode } from 'react'
import { Navigate, useLocation } from 'react-router-dom'
import { useAuth } from '@/features/auth/AuthProvider'
import { FullPageSpinner } from '@/components/layout/FullPageSpinner'

/**
 * Gates everything behind a session. Also enforces the forced password-change
 * flow: an authenticated user with `mustChangePassword` can only see
 * /change-password until it's done (unless `allowMustChange` is set on that
 * one route).
 */
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
    // Already changed (or never needed to) — don't let them linger on that screen.
    return <Navigate to="/" replace />
  }
  return <>{children}</>
}
