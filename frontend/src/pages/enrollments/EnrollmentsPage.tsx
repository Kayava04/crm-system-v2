import { useHasRole } from '@/features/auth/useCan'
import { MyEnrollmentsPage } from './MyEnrollmentsPage'
import { StaffEnrollmentsListPage } from './StaffEnrollmentsListPage'

export function EnrollmentsPage() {
  const isStudent = useHasRole('Student')
  if (isStudent) return <MyEnrollmentsPage />
  return <StaffEnrollmentsListPage />
}
