import { useHasRole } from '@/features/auth/useCan'
import { MyEnrollmentsPage } from './MyEnrollmentsPage'
import { StaffEnrollmentsListPage } from './StaffEnrollmentsListPage'

/** Routes `/enrollments` to the student's own-enrollments view, or the
 * staff list backed by the admin endpoint, based on the current user's role -
 * same pattern as CalendarPage. */
export function EnrollmentsPage() {
  const isStudent = useHasRole('Student')
  if (isStudent) return <MyEnrollmentsPage />
  return <StaffEnrollmentsListPage />
}
