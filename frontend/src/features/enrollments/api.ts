import { api, unwrap } from '@/api/client'
import type { Schemas } from '@/api/types'

export type EnrollmentListItem = Schemas['EnrollmentListResponse']
export type EnrollmentDetail = Schemas['EnrollmentDetailResponse']
export type MyEnrollment = Schemas['MyEnrollmentResponse']
export type CreateEnrollmentInput = Schemas['CreateEnrollmentRequest']
export type EnrollmentStatus = Schemas['EnrollmentStatus']
export type EnrollmentStatusChangeResult = Schemas['EnrollmentStatusChangeResponse']

export interface EnrollmentListFilters {
  studentId?: string
  courseId?: string
  status?: EnrollmentStatus
  page?: number
  pageSize?: number
}

export function getEnrollments(filters: EnrollmentListFilters) {
  return unwrap(
    api.GET('/api/enrollments', {
      params: { query: filters as Record<string, unknown> },
    }),
  )
}

export function getMyEnrollments() {
  return unwrap(api.GET('/api/enrollments/my', {}))
}

export function getEnrollmentById(id: string) {
  return unwrap(api.GET('/api/enrollments/{id}', { params: { path: { id } } }))
}

export function createEnrollment(input: CreateEnrollmentInput) {
  return unwrap(api.POST('/api/enrollments', { body: input }))
}

export function activateEnrollment(id: string) {
  return unwrap(api.PUT('/api/enrollments/{id}/activate', { params: { path: { id } } }))
}

export function suspendEnrollment(id: string) {
  return unwrap(api.PUT('/api/enrollments/{id}/suspend', { params: { path: { id } } }))
}

export function completeEnrollment(id: string) {
  return unwrap(api.PUT('/api/enrollments/{id}/complete', { params: { path: { id } } }))
}

export function terminateEnrollment(id: string) {
  return unwrap(api.PUT('/api/enrollments/{id}/terminate', { params: { path: { id } } }))
}

export function setEnrollmentPrice(id: string, coursePrice: number) {
  return unwrap(
    api.PUT('/api/enrollments/{id}/price', { params: { path: { id } }, body: { coursePrice } }),
  )
}

export function applyEnrollmentDiscount(id: string, discountedPrice: number) {
  return unwrap(
    api.PUT('/api/enrollments/{id}/discount', {
      params: { path: { id } },
      body: { discountedPrice },
    }),
  )
}

export function removeEnrollmentDiscount(id: string) {
  return unwrap(api.DELETE('/api/enrollments/{id}/discount', { params: { path: { id } } }))
}

export function updateEnrollmentComment(id: string, comment: string | null) {
  return unwrap(
    api.PATCH('/api/enrollments/{id}/comment', { params: { path: { id } }, body: { comment } }),
  )
}

export function updateEnrollmentPreferredSchedule(id: string, preferredSchedule: string | null) {
  return unwrap(
    api.PATCH('/api/enrollments/{id}/preferred-schedule', {
      params: { path: { id } },
      body: { preferredSchedule },
    }),
  )
}
