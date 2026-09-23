import { api, unwrap } from '@/api/client'
import type { Schemas } from '@/api/types'

export type ScheduleListItem = Schemas['ScheduleListResponse']
export type ScheduleDetail = Schemas['ScheduleDetailResponse']
export type ScheduleStatus = Schemas['ScheduleStatus']
export type CreateScheduleInput = Schemas['CreateScheduleRequest']
export type LessonSlot = Schemas['LessonSlot']
export type CalendarResponse = Schemas['CalendarResponse']
export type CalendarItem = Schemas['CalendarItemResponse']

export interface ScheduleListFilters {
  enrollmentId?: string
  groupId?: string
  teacherId?: string
  status?: ScheduleStatus
  dateFrom?: string
  dateTo?: string
  page?: number
  pageSize?: number
}

export function getSchedules(filters: ScheduleListFilters) {
  return unwrap(
    api.GET('/api/schedules', {
      params: { query: filters as Record<string, unknown> },
    }),
  )
}

export function getScheduleById(id: string) {
  return unwrap(api.GET('/api/schedules/{id}', { params: { path: { id } } }))
}

export function createSchedule(input: CreateScheduleInput) {
  return unwrap(api.POST('/api/schedules', { body: input }))
}

export function updateSchedule(id: string, input: Schemas['UpdateScheduleRequest']) {
  return unwrap(api.PUT('/api/schedules/{id}', { params: { path: { id } }, body: input }))
}

export function rescheduleLesson(id: string, scheduledDate: string) {
  return unwrap(
    api.PUT('/api/schedules/{id}/reschedule', {
      params: { path: { id } },
      body: { scheduledDate },
    }),
  )
}

export function cancelLesson(id: string) {
  return unwrap(api.PUT('/api/schedules/{id}/cancel', { params: { path: { id } } }))
}

export function completeLesson(id: string) {
  return unwrap(api.PUT('/api/schedules/{id}/complete', { params: { path: { id } } }))
}

export function generateSchedule(input: Schemas['GenerateScheduleRequest']) {
  return unwrap(api.POST('/api/schedules/generate', { body: input }))
}

export function cancelFutureSchedule(input: Schemas['CancelFutureScheduleRequest']) {
  return unwrap(api.PUT('/api/schedules/cancel-future', { body: input }))
}

export function reassignTeacher(input: Schemas['ReassignTeacherRequest']) {
  return unwrap(api.PUT('/api/schedules/reassign-teacher', { body: input }))
}

export function getMyCalendar(from?: string, to?: string) {
  return unwrap(
    api.GET('/api/calendar/my', {
      params: { query: { from, to } as Record<string, unknown> },
    }),
  )
}
