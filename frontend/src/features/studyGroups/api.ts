import { api, unwrap } from '@/api/client'
import type { Schemas } from '@/api/types'

export type GroupListItem = Schemas['GroupListResponse']
export type GroupDetail = Schemas['GroupDetailResponse']
export type GroupMember = Schemas['GroupMemberResponse']
export type CreateGroupInput = Schemas['CreateGroupRequest']
export type UpdateGroupInput = Schemas['UpdateGroupRequest']

export interface GroupListFilters {
  courseId?: string
  teacherId?: string
  page?: number
  pageSize?: number
}

export function getGroups(filters: GroupListFilters) {
  return unwrap(
    api.GET('/api/study-groups', {
      params: { query: filters as Record<string, unknown> },
    }),
  )
}

export function getGroupById(id: string) {
  return unwrap(api.GET('/api/study-groups/{id}', { params: { path: { id } } }))
}

export function createGroup(input: CreateGroupInput) {
  return unwrap(api.POST('/api/study-groups', { body: input }))
}

export function updateGroup(id: string, input: UpdateGroupInput) {
  return unwrap(api.PUT('/api/study-groups/{id}', { params: { path: { id } }, body: input }))
}

export function addGroupMember(id: string, enrollmentId: string) {
  return unwrap(
    api.POST('/api/study-groups/{id}/members', {
      params: { path: { id } },
      body: { enrollmentId },
    }),
  )
}

export function removeGroupMember(id: string, enrollmentId: string) {
  return unwrap(
    api.DELETE('/api/study-groups/{id}/members/{enrollmentId}', {
      params: { path: { id, enrollmentId } },
    }),
  )
}

/** Fetches every group (small, bounded list) for use in selects/lookups. */
export async function getAllGroupsForLookup() {
  const result = await getGroups({ page: 1, pageSize: 200 })
  return result.items
}
