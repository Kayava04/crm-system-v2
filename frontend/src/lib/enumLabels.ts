import type { TFunction } from 'i18next'

export type EnumCategory =
  | 'studentStatus'
  | 'teacherStatus'
  | 'learningGoal'
  | 'format'
  | 'lessonType'
  | 'level'
  | 'language'
  | 'courseStatus'
  | 'enrollmentStatus'
  | 'scheduleStatus'
  | 'dayOfWeek'
  | 'invoiceStatus'
  | 'payrollStatus'
  | 'notificationType'
  | 'broadcastAudience'
  | 'materialType'

export function enumLabel(
  t: TFunction,
  category: EnumCategory,
  value: string | null | undefined,
): string {
  if (!value) return '—'
  return t(`enums.${category}.${value}`, { defaultValue: value })
}
