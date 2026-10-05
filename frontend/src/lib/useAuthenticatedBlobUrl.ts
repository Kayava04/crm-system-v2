import { useEffect, useState } from 'react'
import { tokenStore } from '@/api/tokenStore'
import { API_BASE_URL } from '@/api/client'

export function useAuthenticatedBlobUrl(path: string | null): string | null {
  const [url, setUrl] = useState<string | null>(null)

  useEffect(() => {
    if (!path) return
    let objectUrl: string | null = null
    let cancelled = false

    async function load() {
      const token = tokenStore.getAccessToken()
      try {
        const res = await fetch(`${API_BASE_URL}${path}`, {
          headers: token ? { Authorization: `Bearer ${token}` } : undefined,
        })
        if (!res.ok || cancelled) return
        const blob = await res.blob()
        objectUrl = URL.createObjectURL(blob)
        if (!cancelled) setUrl(objectUrl)
      } catch {
        return
      }
    }
    void load()

    return () => {
      cancelled = true
      if (objectUrl) URL.revokeObjectURL(objectUrl)
    }
  }, [path])

  return path ? url : null
}
