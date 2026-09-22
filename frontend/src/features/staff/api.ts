import { api, unwrap } from '@/api/client'
import type { Schemas } from '@/api/types'

export type StaffMember = Schemas['StaffMember']
export type PermissionOption = Schemas['PermissionResponse']

export function getStaff(isActive?: boolean) {
  return unwrap(
    api.GET('/api/auth/users', {
      params: { query: { isActive } as Record<string, unknown> },
    }),
  )
}

export function getPermissionOptions() {
  return unwrap(api.GET('/api/auth/permissions', {}))
}

export interface CreateStaffInput {
  email: string
  firstName?: string | null
  lastName?: string | null
  middleName?: string | null
  phoneNumber?: string | null
  dateOfBirth?: string | null
  city?: string | null
  country?: string | null
  salary?: number | null
  permissionIds: string[]
}

/** Creates a new Admin-role staff account (never SuperAdmin — minting another
 * SuperAdmin from this permission-gated screen would be a privilege-escalation
 * risk the brief doesn't call for, so the role is fixed here rather than exposed
 * as a picker). Salary can also be changed later with setStaffSalary. */
export function registerStaff(input: CreateStaffInput) {
  return unwrap(
    api.POST('/api/auth/register', {
      body: {
        email: input.email,
        role: 'Admin',
        permissionIds: input.permissionIds,
        profileType: null,
        profileId: null,
        firstName: input.firstName || null,
        lastName: input.lastName || null,
        middleName: input.middleName || null,
        phoneNumber: input.phoneNumber || null,
        dateOfBirth: input.dateOfBirth || null,
        city: input.city || null,
        country: input.country || null,
        salary: input.salary ?? null,
      },
    }),
  )
}

export function setStaffPermissions(id: string, permissionIds: string[]) {
  return unwrap(
    api.PUT('/api/auth/users/{id}/permissions', {
      params: { path: { id } },
      body: { permissionIds },
    }),
  )
}

export function setStaffStatus(id: string, isActive: boolean) {
  return unwrap(
    api.PUT('/api/auth/users/{id}/status', {
      params: { path: { id } },
      body: { isActive },
    }),
  )
}

export function resetStaffPassword(id: string) {
  return unwrap(api.POST('/api/auth/users/{id}/reset-password', { params: { path: { id } } }))
}

export function setStaffSalary(id: string, salary: number | null) {
  return unwrap(
    api.PUT('/api/auth/users/{id}/salary', {
      params: { path: { id } },
      body: { salary },
    }),
  )
}
