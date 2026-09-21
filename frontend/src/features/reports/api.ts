import { api, unwrap } from '@/api/client'
import type { Schemas } from '@/api/types'

export type BillingSummary = Schemas['BillingSummaryResponse']
export type EnrollmentsSummary = Schemas['EnrollmentsSummaryResponse']
export type StudentsSummary = Schemas['StudentsSummaryResponse']
export type TeachersSummary = Schemas['TeachersSummaryResponse']

export function getBillingSummary(dateFrom?: string, dateTo?: string) {
  return unwrap(
    api.GET('/api/reports/billing/summary', {
      params: { query: { dateFrom, dateTo } as Record<string, unknown> },
    }),
  )
}

export function getEnrollmentsSummary() {
  return unwrap(api.GET('/api/reports/enrollments/summary', {}))
}

export function getStudentsSummary() {
  return unwrap(api.GET('/api/reports/students/summary', {}))
}

export function getTeachersSummary() {
  return unwrap(api.GET('/api/reports/teachers/summary', {}))
}
