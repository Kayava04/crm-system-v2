import { api, unwrap } from '@/api/client'
import type { Schemas } from '@/api/types'

export type StudentListItem = Schemas['StudentListResponse']
export type StudentDetail = Schemas['StudentDetailResponse']
export type CreateStudentInput = Schemas['CreateRequest']
export type UpdateStudentInput = Schemas['UpdateRequest']
export type UpdatePreferencesInput = Schemas['UpdatePreferencesRequest']
export type ParentInfoInput = Schemas['AddParentInfoRequest']
export type StudentStatus = Schemas['StudentStatus']
export type ChangeStudentStatusResult = Schemas['ChangeStudentStatusResponse']

export interface StudentListFilters {
  search?: string
  city?: string
  isChild?: boolean
  language?: Schemas['Language']
  currentLevel?: Schemas['Level']
  format?: Schemas['Format']
  page?: number
  pageSize?: number
}

export function getStudents(filters: StudentListFilters) {
  return unwrap(
    api.GET('/api/students', {
      params: { query: filters as Record<string, unknown> },
    }),
  )
}

export function getStudentById(id: string) {
  return unwrap(api.GET('/api/students/{id}', { params: { path: { id } } }))
}

export function createStudent(input: CreateStudentInput) {
  return unwrap(api.POST('/api/students', { body: input }))
}

export function updateStudent(id: string, input: UpdateStudentInput) {
  return unwrap(api.PUT('/api/students/{id}', { params: { path: { id } }, body: input }))
}

export function updateStudentPreferences(id: string, input: UpdatePreferencesInput) {
  return unwrap(
    api.PUT('/api/students/{id}/preferences', { params: { path: { id } }, body: input }),
  )
}

export function updateStudentComment(id: string, comment: string | null) {
  return unwrap(
    api.PATCH('/api/students/{id}/comment', { params: { path: { id } }, body: { comment } }),
  )
}

export function changeStudentStatus(id: string, status: StudentStatus) {
  return unwrap(
    api.PUT('/api/students/{id}/status', { params: { path: { id } }, body: { status } }),
  )
}

export function deleteStudent(id: string) {
  return unwrap(api.DELETE('/api/students/{id}', { params: { path: { id } } }))
}

export function addParentInfo(id: string, input: ParentInfoInput) {
  return unwrap(
    api.POST('/api/students/{id}/parent-info', { params: { path: { id } }, body: input }),
  )
}

export function updateParentInfo(id: string, input: ParentInfoInput) {
  return unwrap(
    api.PUT('/api/students/{id}/parent-info', { params: { path: { id } }, body: input }),
  )
}

export function deleteParentInfo(id: string) {
  return unwrap(api.DELETE('/api/students/{id}/parent-info', { params: { path: { id } } }))
}

export function bulkDeleteStudents(ids: string[]) {
  return unwrap(api.DELETE('/api/students/bulk', { body: { ids } }))
}

export function getStudentImportTemplateUrl(fileFormat: 'xlsx' | 'json', lang: 'uk' | 'en') {
  const params = new URLSearchParams({ fileFormat, lang })
  return `/api/students/import-template?${params.toString()}`
}

export function getStudentExportUrl(
  filters: StudentListFilters,
  fileFormat: 'xlsx' | 'json',
  lang: 'uk' | 'en',
) {
  const params = new URLSearchParams({ fileFormat, lang })
  for (const [key, value] of Object.entries(filters)) {
    if (value !== undefined && value !== '') params.set(key, String(value))
  }
  return `/api/students/export?${params.toString()}`
}

export async function importStudents(
  file: File,
  options: { dryRun: boolean; allOrNothing: boolean },
) {
  const formData = new FormData()
  formData.append('file', file)
  return unwrap(
    api.POST('/api/students/import', {
      params: { query: options },
      body: formData as unknown as { file?: string },
      bodySerializer: (body) => body as unknown as BodyInit,
    }),
  )
}
