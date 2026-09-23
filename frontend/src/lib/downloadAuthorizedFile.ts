import { tokenStore } from '@/api/tokenStore'
import { API_BASE_URL } from '@/api/client'
import { toApiError } from '@/api/errors'

function filenameFromContentDisposition(header: string | null): string | null {
  if (!header) return null
  const match = /filename\*?=(?:UTF-8''|")?([^;"]+)/i.exec(header)
  return match ? decodeURIComponent(match[1].replace(/"/g, '')) : null
}

/** Downloads a file that needs the Authorization header (exports, import
 * templates) and saves it via a temporary <a download>. */
export async function downloadAuthorizedFile(path: string, fallbackFilename: string) {
  const token = tokenStore.getAccessToken()
  const res = await fetch(`${API_BASE_URL}${path}`, {
    headers: token ? { Authorization: `Bearer ${token}` } : undefined,
  })
  if (!res.ok) throw await toApiError(res)

  const blob = await res.blob()
  const filename =
    filenameFromContentDisposition(res.headers.get('Content-Disposition')) ?? fallbackFilename
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = filename
  document.body.appendChild(link)
  link.click()
  link.remove()
  URL.revokeObjectURL(url)
}
