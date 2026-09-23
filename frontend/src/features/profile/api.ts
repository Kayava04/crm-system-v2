import { api, unwrap } from '@/api/client'
import type { Schemas } from '@/api/types'

export type StudentDetail = Schemas['StudentDetailResponse']
export type TeacherDetail = Schemas['TeacherDetailResponse']

export function getMyStudentProfile() {
  return unwrap(api.GET('/api/students/me', {}))
}

export function getMyTeacherProfile() {
  return unwrap(api.GET('/api/teachers/me', {}))
}

export async function uploadMyPhoto(file: File) {
  const formData = new FormData()
  formData.append('file', file)
  // openapi-fetch passes a FormData body through untouched (lets the browser set
  // the multipart boundary); the generated type expects a plain object, hence the cast.
  return unwrap(
    api.PUT('/api/auth/me/photo', {
      body: formData as unknown as { file?: string },
      bodySerializer: (body) => body as unknown as BodyInit,
    }),
  )
}

export function deleteMyPhoto() {
  return unwrap(api.DELETE('/api/auth/me/photo', {}))
}
