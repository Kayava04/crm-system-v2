import { api, unwrap } from '@/api/client'
import type { Schemas } from '@/api/types'

export type LoginResponse = Schemas['LoginResponse']
export type ChangePasswordInput = Schemas['ChangePasswordRequest']
export type SystemRole = Schemas['SystemRole']
export type RegisterResult = Schemas['RegisterResponse']

export interface CreateProfileAccountInput {
  email: string
  role: 'Student' | 'Teacher'
  profileType: 'Student' | 'Teacher'
  profileId: string
}

export function login(email: string, password: string) {
  return unwrap(api.POST('/api/auth/login', { body: { email, password } }))
}

export function fetchMe() {
  return unwrap(api.GET('/api/auth/me', {}))
}

export function changePassword(input: ChangePasswordInput) {
  return unwrap(api.POST('/api/auth/change-password', { body: input }))
}

export type MeContact = Schemas['MeContact']

export function updateMyContact(input: Schemas['UpdateMyContactRequest']) {
  return unwrap(api.PUT('/api/auth/me/contact', { body: input }))
}

export function createProfileAccount(input: CreateProfileAccountInput) {
  return unwrap(
    api.POST('/api/auth/register', {
      body: {
        email: input.email,
        role: input.role,
        permissionIds: null,
        profileType: input.profileType,
        profileId: input.profileId,
        firstName: null,
        lastName: null,
        phoneNumber: null,
      },
    }),
  )
}
