import { api, unwrap } from '@/api/client'
import type { Schemas } from '@/api/types'

export type TeacherListItem = Schemas['TeacherListResponse']
export type TeacherDetail = Schemas['TeacherDetailResponse']
export type CreateTeacherInput = Schemas['CreateTeacherRequest']
export type UpdateTeacherInput = Schemas['UpdateTeacherRequest']
export type AddSalaryRateInput = Schemas['AddSalaryRateRequest']
export type TeacherStatus = Schemas['TeacherStatus']
export type ChangeTeacherStatusResult = Schemas['ChangeTeacherStatusResponse']

export interface TeacherListFilters {
  search?: string
  city?: string
  status?: TeacherStatus
  page?: number
  pageSize?: number
}

export function getTeachers(filters: TeacherListFilters) {
  return unwrap(
    api.GET('/api/teachers', {
      params: { query: filters as Record<string, unknown> },
    }),
  )
}

export function getTeacherById(id: string) {
  return unwrap(api.GET('/api/teachers/{id}', { params: { path: { id } } }))
}

export function createTeacher(input: CreateTeacherInput) {
  return unwrap(api.POST('/api/teachers', { body: input }))
}

export function updateTeacher(id: string, input: UpdateTeacherInput) {
  return unwrap(api.PUT('/api/teachers/{id}', { params: { path: { id } }, body: input }))
}

export function updateTeacherComment(id: string, comment: string | null) {
  return unwrap(
    api.PATCH('/api/teachers/{id}/comment', { params: { path: { id } }, body: { comment } }),
  )
}

export function changeTeacherStatus(id: string, status: TeacherStatus) {
  return unwrap(
    api.PUT('/api/teachers/{id}/status', { params: { path: { id } }, body: { status } }),
  )
}

export function deleteTeacher(id: string) {
  return unwrap(api.DELETE('/api/teachers/{id}', { params: { path: { id } } }))
}

export function addSalaryRate(id: string, input: AddSalaryRateInput) {
  return unwrap(
    api.POST('/api/teachers/{id}/salary-rates', { params: { path: { id } }, body: input }),
  )
}

export function bulkDeleteTeachers(ids: string[]) {
  return unwrap(api.DELETE('/api/teachers/bulk', { body: { ids } }))
}

export function getTeacherImportTemplateUrl(fileFormat: 'xlsx' | 'json', lang: 'uk' | 'en') {
  const params = new URLSearchParams({ fileFormat, lang })
  return `/api/teachers/import-template?${params.toString()}`
}

export function getTeacherExportUrl(
  filters: TeacherListFilters,
  fileFormat: 'xlsx' | 'json',
  lang: 'uk' | 'en',
) {
  const params = new URLSearchParams({ fileFormat, lang })
  for (const [key, value] of Object.entries(filters)) {
    if (value !== undefined && value !== '') params.set(key, String(value))
  }
  return `/api/teachers/export?${params.toString()}`
}

export async function importTeachers(
  file: File,
  options: { dryRun: boolean; allOrNothing: boolean },
) {
  const formData = new FormData()
  formData.append('file', file)
  return unwrap(
    api.POST('/api/teachers/import', {
      params: { query: options },
      body: formData as unknown as { file?: string },
      bodySerializer: (body) => body as unknown as BodyInit,
    }),
  )
}
