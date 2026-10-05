import { useHasRole } from '@/features/auth/useCan'
import { MyCalendarView } from './MyCalendarView'
import { StaffCalendarView } from './StaffCalendarView'

export function CalendarPage() {
  const isTeacher = useHasRole('Teacher')
  const isStudent = useHasRole('Student')

  if (isTeacher || isStudent) return <MyCalendarView />
  return <StaffCalendarView />
}
