import { api, unwrap } from '@/api/client'
import type { Schemas } from '@/api/types'

export type MaterialListItem = Schemas['MaterialListResponse']
export type MaterialDetail = Schemas['MaterialDetailResponse']
export type MaterialType = Schemas['MaterialType']

export interface MaterialListFilters {
  courseId?: string
  type?: MaterialType
  search?: string
  page?: number
  pageSize?: number
}

export function getMaterials(filters: MaterialListFilters) {
  return unwrap(
    api.GET('/api/materials', {
      params: { query: filters as Record<string, unknown> },
    }),
  )
}

export function getMaterialById(id: string) {
  return unwrap(api.GET('/api/materials/{id}', { params: { path: { id } } }))
}

export function createMaterial(input: Schemas['CreateMaterialRequest']) {
  return unwrap(api.POST('/api/materials', { body: input }))
}

export function updateMaterial(id: string, input: Schemas['UpdateMaterialRequest']) {
  return unwrap(api.PUT('/api/materials/{id}', { params: { path: { id } }, body: input }))
}

export function deleteMaterial(id: string) {
  return unwrap(api.DELETE('/api/materials/{id}', { params: { path: { id } } }))
}
