import { useMemo, type ReactNode } from 'react'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { tokenStore } from '@/api/tokenStore'
import { fetchMe, login as loginRequest, changePassword as changePasswordRequest } from './api'
import { AuthContext, type AuthContextValue, type AuthStatus } from './useAuth'

const ME_QUERY_KEY = ['auth', 'me'] as const

export function AuthProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient()
  const hasRefreshToken = tokenStore.getRefreshToken() !== null

  const meQuery = useQuery({
    queryKey: ME_QUERY_KEY,
    queryFn: fetchMe,
    retry: false,
    enabled: hasRefreshToken,
    staleTime: 60_000,
    refetchOnWindowFocus: false,
  })

  const status: AuthStatus = !hasRefreshToken
    ? 'unauthenticated'
    : meQuery.isPending
      ? 'loading'
      : meQuery.data
        ? 'authenticated'
        : 'unauthenticated'

  const value = useMemo<AuthContextValue>(
    () => ({
      user: meQuery.data ?? null,
      status,
      async login(email, password) {
        const res = await loginRequest(email, password)
        tokenStore.setTokens(res.accessToken, res.refreshToken)
        const me = await queryClient.fetchQuery({ queryKey: ME_QUERY_KEY, queryFn: fetchMe })
        queryClient.setQueryData(ME_QUERY_KEY, me)
        return me
      },
      logout() {
        tokenStore.clear()
        queryClient.setQueryData(ME_QUERY_KEY, null)
        queryClient.clear()
      },
      async changePassword(input) {
        await changePasswordRequest(input)
        await queryClient.invalidateQueries({ queryKey: ME_QUERY_KEY })
      },
    }),
    [meQuery.data, status, queryClient],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
