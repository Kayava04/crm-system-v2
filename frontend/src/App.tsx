import { lazy, Suspense } from 'react'
import { Routes, Route } from 'react-router-dom'
import { RequireAuth } from '@/routes/RequireAuth'
import { RequireAccess } from '@/routes/RequireAccess'
import { AppShell } from '@/components/layout/AppShell'
import { PageSpinner } from '@/components/layout/PageSpinner'
import { LoginPage } from '@/pages/auth/LoginPage'
import { ChangePasswordPage } from '@/pages/auth/ChangePasswordPage'
import { ForbiddenPage } from '@/pages/errors/ForbiddenPage'
import { NotFoundPage } from '@/pages/errors/NotFoundPage'
import { HomePage } from '@/pages/HomePage'

// Every page behind the app shell is loaded on demand: nobody pays for the staff, billing or
// reporting screens' code just to see their own calendar. Login, the shell and the error pages
// stay eager since they are on the critical path for everyone.
const ProfilePage = lazy(() => import('@/pages/profile/ProfilePage').then((m) => ({ default: m.ProfilePage })))
const StudentsListPage = lazy(() =>
  import('@/pages/students/StudentsListPage').then((m) => ({ default: m.StudentsListPage })),
)
const StudentCreatePage = lazy(() =>
  import('@/pages/students/StudentCreatePage').then((m) => ({ default: m.StudentCreatePage })),
)
const StudentDetailPage = lazy(() =>
  import('@/pages/students/StudentDetailPage').then((m) => ({ default: m.StudentDetailPage })),
)
const TeachersListPage = lazy(() =>
  import('@/pages/teachers/TeachersListPage').then((m) => ({ default: m.TeachersListPage })),
)
const TeacherCreatePage = lazy(() =>
  import('@/pages/teachers/TeacherCreatePage').then((m) => ({ default: m.TeacherCreatePage })),
)
const TeacherDetailPage = lazy(() =>
  import('@/pages/teachers/TeacherDetailPage').then((m) => ({ default: m.TeacherDetailPage })),
)
const MyStudentsPage = lazy(() =>
  import('@/pages/teachers/MyStudentsPage').then((m) => ({ default: m.MyStudentsPage })),
)
const CoursesListPage = lazy(() =>
  import('@/pages/courses/CoursesListPage').then((m) => ({ default: m.CoursesListPage })),
)
const CourseCreatePage = lazy(() =>
  import('@/pages/courses/CourseCreatePage').then((m) => ({ default: m.CourseCreatePage })),
)
const CourseDetailPage = lazy(() =>
  import('@/pages/courses/CourseDetailPage').then((m) => ({ default: m.CourseDetailPage })),
)
const EnrollmentDetailPage = lazy(() =>
  import('@/pages/enrollments/EnrollmentDetailPage').then((m) => ({ default: m.EnrollmentDetailPage })),
)
const EnrollmentsPage = lazy(() =>
  import('@/pages/enrollments/EnrollmentsPage').then((m) => ({ default: m.EnrollmentsPage })),
)
const CalendarPage = lazy(() => import('@/pages/calendar/CalendarPage').then((m) => ({ default: m.CalendarPage })))
const StudyGroupsListPage = lazy(() =>
  import('@/pages/studyGroups/StudyGroupsListPage').then((m) => ({ default: m.StudyGroupsListPage })),
)
const StudyGroupCreatePage = lazy(() =>
  import('@/pages/studyGroups/StudyGroupCreatePage').then((m) => ({ default: m.StudyGroupCreatePage })),
)
const StudyGroupDetailPage = lazy(() =>
  import('@/pages/studyGroups/StudyGroupDetailPage').then((m) => ({ default: m.StudyGroupDetailPage })),
)
const InvoicesListPage = lazy(() =>
  import('@/pages/billing/InvoicesListPage').then((m) => ({ default: m.InvoicesListPage })),
)
const MyInvoicesPage = lazy(() =>
  import('@/pages/billing/MyInvoicesPage').then((m) => ({ default: m.MyInvoicesPage })),
)
const PayrollsListPage = lazy(() =>
  import('@/pages/billing/PayrollsListPage').then((m) => ({ default: m.PayrollsListPage })),
)
const MyPayrollsPage = lazy(() =>
  import('@/pages/billing/MyPayrollsPage').then((m) => ({ default: m.MyPayrollsPage })),
)
const NotificationsPage = lazy(() =>
  import('@/pages/notifications/NotificationsPage').then((m) => ({ default: m.NotificationsPage })),
)
const AdminNotificationsPage = lazy(() =>
  import('@/pages/notifications/AdminNotificationsPage').then((m) => ({ default: m.AdminNotificationsPage })),
)
const MaterialsListPage = lazy(() =>
  import('@/pages/materials/MaterialsListPage').then((m) => ({ default: m.MaterialsListPage })),
)
const StaffListPage = lazy(() => import('@/pages/staff/StaffListPage').then((m) => ({ default: m.StaffListPage })))
const DashboardPage = lazy(() =>
  import('@/pages/reports/DashboardPage').then((m) => ({ default: m.DashboardPage })),
)

// Wraps a lazily-loaded page's element with the shared Suspense fallback, so every route below reads
// the same way whether or not it happens to be code-split.
function Lazy({ children }: { children: React.ReactNode }) {
  return <Suspense fallback={<PageSpinner />}>{children}</Suspense>
}

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route
        path="/change-password"
        element={
          <RequireAuth allowMustChange>
            <ChangePasswordPage />
          </RequireAuth>
        }
      />

      <Route
        element={
          <RequireAuth>
            <AppShell />
          </RequireAuth>
        }
      >
        <Route index element={<HomePage />} />

        <Route
          path="dashboard"
          element={
            <RequireAccess permission="CanViewReports">
              <Lazy>
                <DashboardPage />
              </Lazy>
            </RequireAccess>
          }
        />
        <Route
          path="calendar"
          element={
            <RequireAccess permission="CanViewSchedule" roles={['Teacher', 'Student']}>
              <Lazy>
                <CalendarPage />
              </Lazy>
            </RequireAccess>
          }
        />
        <Route
          path="students"
          element={
            <RequireAccess permission="CanViewStudents">
              <Lazy>
                <StudentsListPage />
              </Lazy>
            </RequireAccess>
          }
        />
        <Route
          path="students/new"
          element={
            <RequireAccess permission="CanCreateStudents">
              <Lazy>
                <StudentCreatePage />
              </Lazy>
            </RequireAccess>
          }
        />
        <Route
          path="students/:id"
          element={
            <RequireAccess permission="CanViewStudents">
              <Lazy>
                <StudentDetailPage />
              </Lazy>
            </RequireAccess>
          }
        />
        <Route
          path="teachers"
          element={
            <RequireAccess permission="CanViewTeachers">
              <Lazy>
                <TeachersListPage />
              </Lazy>
            </RequireAccess>
          }
        />
        <Route
          path="teachers/new"
          element={
            <RequireAccess permission="CanCreateTeachers">
              <Lazy>
                <TeacherCreatePage />
              </Lazy>
            </RequireAccess>
          }
        />
        <Route
          path="teachers/:id"
          element={
            <RequireAccess permission="CanViewTeachers">
              <Lazy>
                <TeacherDetailPage />
              </Lazy>
            </RequireAccess>
          }
        />
        <Route
          path="my-students"
          element={
            <RequireAccess roles={['Teacher']}>
              <Lazy>
                <MyStudentsPage />
              </Lazy>
            </RequireAccess>
          }
        />
        <Route
          path="courses"
          element={
            <RequireAccess permission="CanViewCourses">
              <Lazy>
                <CoursesListPage />
              </Lazy>
            </RequireAccess>
          }
        />
        <Route
          path="courses/new"
          element={
            <RequireAccess permission="CanManageCourses">
              <Lazy>
                <CourseCreatePage />
              </Lazy>
            </RequireAccess>
          }
        />
        <Route
          path="courses/:id"
          element={
            <RequireAccess permission="CanViewCourses">
              <Lazy>
                <CourseDetailPage />
              </Lazy>
            </RequireAccess>
          }
        />
        <Route
          path="enrollments"
          element={
            <RequireAccess permission="CanViewEnrollments" roles={['Student']}>
              <Lazy>
                <EnrollmentsPage />
              </Lazy>
            </RequireAccess>
          }
        />
        <Route
          path="enrollments/:id"
          element={
            <RequireAccess permission="CanViewEnrollments">
              <Lazy>
                <EnrollmentDetailPage />
              </Lazy>
            </RequireAccess>
          }
        />
        <Route
          path="study-groups"
          element={
            <RequireAccess permission="CanViewSchedule">
              <Lazy>
                <StudyGroupsListPage />
              </Lazy>
            </RequireAccess>
          }
        />
        <Route
          path="study-groups/new"
          element={
            <RequireAccess permission="CanManageSchedule">
              <Lazy>
                <StudyGroupCreatePage />
              </Lazy>
            </RequireAccess>
          }
        />
        <Route
          path="study-groups/:id"
          element={
            <RequireAccess permission="CanViewSchedule">
              <Lazy>
                <StudyGroupDetailPage />
              </Lazy>
            </RequireAccess>
          }
        />
        <Route
          path="billing/invoices"
          element={
            <RequireAccess permission="CanViewPayments">
              <Lazy>
                <InvoicesListPage />
              </Lazy>
            </RequireAccess>
          }
        />
        <Route
          path="billing/payroll"
          element={
            <RequireAccess permission="CanViewPayments">
              <Lazy>
                <PayrollsListPage />
              </Lazy>
            </RequireAccess>
          }
        />
        <Route
          path="invoices"
          element={
            <RequireAccess roles={['Student']}>
              <Lazy>
                <MyInvoicesPage />
              </Lazy>
            </RequireAccess>
          }
        />
        <Route
          path="payroll"
          element={
            <RequireAccess roles={['Teacher']}>
              <Lazy>
                <MyPayrollsPage />
              </Lazy>
            </RequireAccess>
          }
        />
        <Route
          path="materials"
          element={
            <RequireAccess permission="CanViewMaterials" roles={['Teacher', 'Student']}>
              <Lazy>
                <MaterialsListPage />
              </Lazy>
            </RequireAccess>
          }
        />
        <Route
          path="notifications"
          element={
            <Lazy>
              <NotificationsPage />
            </Lazy>
          }
        />
        <Route
          path="notifications/admin"
          element={
            <RequireAccess permission="CanManageNotifications">
              <Lazy>
                <AdminNotificationsPage />
              </Lazy>
            </RequireAccess>
          }
        />
        <Route
          path="staff"
          element={
            <RequireAccess permission="CanManageAdmins">
              <Lazy>
                <StaffListPage />
              </Lazy>
            </RequireAccess>
          }
        />
        <Route
          path="profile"
          element={
            <Lazy>
              <ProfilePage />
            </Lazy>
          }
        />
        <Route path="403" element={<ForbiddenPage />} />
      </Route>

      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  )
}
