import { api, unwrap } from '@/api/client'
import type { Schemas } from '@/api/types'

export type CalendarEvent = Schemas['CalendarEventResponse']
export type CalendarEventVisibility = Schemas['CalendarEventVisibility']

export interface CalendarEventInput {
  visibility: CalendarEventVisibility
  title: string
  description: string | null
  startsAt: string
  endsAt: string
  isAllDay: boolean
}

// Arbitrary calendar entries that are not lessons: a personal reminder only its
// owner ever sees, or (for a CanManageSchedule holder) a notice everyone sees.
export function getCalendarEvents(from?: string, to?: string) {
  return unwrap(
    api.GET('/api/calendar/events', {
      params: { query: { from, to } as Record<string, unknown> },
    }),
  )
}

export function createCalendarEvent(input: CalendarEventInput) {
  return unwrap(api.POST('/api/calendar/events', { body: input }))
}

export function updateCalendarEvent(id: string, input: CalendarEventInput) {
  return unwrap(api.PUT('/api/calendar/events/{id}', { params: { path: { id } }, body: input }))
}

export function deleteCalendarEvent(id: string) {
  return unwrap(api.DELETE('/api/calendar/events/{id}', { params: { path: { id } } }))
}
