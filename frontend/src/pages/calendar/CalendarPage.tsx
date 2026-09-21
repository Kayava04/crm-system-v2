import { useHasRole } from '@/features/auth/useCan'
import { MyCalendarView } from './MyCalendarView'
import { StaffCalendarView } from './StaffCalendarView'

/** Routes to the student/teacher self-service week view, or the staff
 * scheduling view, based on the current user's role. */
export function CalendarPage() {
  const isTeacher = useHasRole('Teacher')
  const isStudent = useHasRole('Student')

  if (isTeacher || isStudent) return <MyCalendarView />
  return <StaffCalendarView />
}
