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

/** Translates a backend enum value (e.g. "Active", "B1") via the `enums.*`
 * locale namespace, falling back to the raw value for anything unmapped. */
export function enumLabel(
  t: TFunction,
  category: EnumCategory,
  value: string | null | undefined,
): string {
  if (!value) return '—'
  return t(`enums.${category}.${value}`, { defaultValue: value })
}
