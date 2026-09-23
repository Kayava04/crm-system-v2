import { createContext, useContext } from 'react'
import type { MeResponse } from '@/api/types'
import type { Schemas } from '@/api/types'

export type AuthStatus = 'loading' | 'authenticated' | 'unauthenticated'

export interface AuthContextValue {
  user: MeResponse | null
  status: AuthStatus
  login: (email: string, password: string) => Promise<MeResponse>
  logout: () => void
  changePassword: (input: Schemas['ChangePasswordRequest']) => Promise<void>
}

export const AuthContext = createContext<AuthContextValue | null>(null)

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used within AuthProvider')
  return ctx
}
