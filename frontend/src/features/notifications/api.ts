import { api, unwrap } from '@/api/client'
import type { Schemas } from '@/api/types'

export type Notification = Schemas['NotificationResponse']
export type AdminNotification = Schemas['AdminNotificationResponse']
export type NotificationType = Schemas['NotificationType']
export type BroadcastAudience = Schemas['BroadcastAudience']

export async function getUnreadCount(): Promise<number> {
  const res = await unwrap(api.GET('/api/notifications/unread-count', {}))
  return Number(res.count)
}

export interface MyNotificationFilters {
  unreadOnly?: boolean
  page?: number
  pageSize?: number
}

export function getMyNotifications(filters: MyNotificationFilters) {
  return unwrap(
    api.GET('/api/notifications', {
      params: { query: filters as Record<string, unknown> },
    }),
  )
}

export function markNotificationRead(id: string) {
  return unwrap(api.PUT('/api/notifications/{id}/read', { params: { path: { id } } }))
}

export function markAllNotificationsRead() {
  return unwrap(api.PUT('/api/notifications/read-all', {}))
}

export interface AllNotificationFilters {
  recipientUserId?: string
  type?: NotificationType
  unreadOnly?: boolean
  page?: number
  pageSize?: number
}

export function getAllNotifications(filters: AllNotificationFilters) {
  return unwrap(
    api.GET('/api/notifications/all', {
      params: { query: filters as Record<string, unknown> },
    }),
  )
}

export function broadcastNotification(input: Schemas['BroadcastNotificationRequest']) {
  return unwrap(api.POST('/api/notifications/broadcast', { body: input }))
}

export function sendNotification(input: Schemas['SendNotificationRequest']) {
  return unwrap(api.POST('/api/notifications/send', { body: input }))
}

export function sendInvoiceReminders(input: Schemas['SendInvoiceRemindersRequest']) {
  return unwrap(api.POST('/api/notifications/invoice-reminders', { body: input }))
}

export function sendLessonReminders(input: Schemas['SendLessonRemindersRequest']) {
  return unwrap(api.POST('/api/notifications/lesson-reminders', { body: input }))
}
