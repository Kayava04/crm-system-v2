import { describe, expect, it, vi, beforeEach, afterEach, type Mock } from 'vitest'
import { tokenStore } from '@/api/tokenStore'
import { ApiError } from '@/api/errors'
import { downloadAuthorizedFile } from './downloadAuthorizedFile'

let fetchMock: Mock
let clicked: HTMLAnchorElement[]

function fileResponse(headers: Record<string, string> = {}) {
  return new Response(new Blob(['data']), { status: 200, headers })
}

beforeEach(() => {
  tokenStore.clear()
  fetchMock = vi.fn()
  vi.stubGlobal('fetch', fetchMock)
  URL.createObjectURL = vi.fn(() => 'blob:file')
  URL.revokeObjectURL = vi.fn()
  clicked = []
  vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(function (
    this: HTMLAnchorElement,
  ) {
    clicked.push(this)
  })
})

afterEach(() => {
  vi.unstubAllGlobals()
  vi.restoreAllMocks()
})

describe('downloadAuthorizedFile', () => {
  it('sends the access token with the request', async () => {
    tokenStore.setTokens('access-1', 'refresh-1')
    fetchMock.mockResolvedValue(fileResponse())

    await downloadAuthorizedFile('/api/reports/export', 'report.xlsx')

    const [, init] = fetchMock.mock.calls[0]
    expect(init.headers).toEqual({ Authorization: 'Bearer access-1' })
  })

  it('uses the filename from Content-Disposition', async () => {
    fetchMock.mockResolvedValue(
      fileResponse({ 'Content-Disposition': 'attachment; filename="students.xlsx"' }),
    )

    await downloadAuthorizedFile('/api/students/export', 'fallback.xlsx')

    expect(clicked[0].download).toBe('students.xlsx')
  })

  it('decodes a UTF-8 encoded filename', async () => {
    fetchMock.mockResolvedValue(
      fileResponse({
        'Content-Disposition': "attachment; filename*=UTF-8''%D0%B7%D0%B2%D1%96%D1%82.pdf",
      }),
    )

    await downloadAuthorizedFile('/api/reports/export', 'fallback.pdf')

    expect(clicked[0].download).toBe('звіт.pdf')
  })

  it('falls back to the given filename without Content-Disposition', async () => {
    fetchMock.mockResolvedValue(fileResponse())

    await downloadAuthorizedFile('/api/reports/export', 'fallback.pdf')

    expect(clicked[0].download).toBe('fallback.pdf')
  })

  it('cleans up the temporary link and object URL', async () => {
    fetchMock.mockResolvedValue(fileResponse())

    await downloadAuthorizedFile('/api/reports/export', 'fallback.pdf')

    expect(clicked[0].href).toBe('blob:file')
    expect(clicked[0].isConnected).toBe(false)
    expect(URL.revokeObjectURL).toHaveBeenCalledWith('blob:file')
  })

  it('throws an ApiError and downloads nothing on a failed response', async () => {
    fetchMock.mockResolvedValue(
      new Response(JSON.stringify({ title: 'Forbidden' }), { status: 403 }),
    )

    const error = await downloadAuthorizedFile('/api/reports/export', 'x.pdf').catch((e) => e)

    expect(error).toBeInstanceOf(ApiError)
    expect(error.isForbidden).toBe(true)
    expect(clicked).toHaveLength(0)
  })
})
