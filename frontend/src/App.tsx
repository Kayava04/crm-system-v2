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
import { ProfilePage } from '@/pages/profile/ProfilePage'
import { StudentsListPage } from '@/pages/students/StudentsListPage'
import { StudentCreatePage } from '@/pages/students/StudentCreatePage'
import { StudentDetailPage } from '@/pages/students/StudentDetailPage'
import { TeachersListPage } from '@/pages/teachers/TeachersListPage'
import { TeacherCreatePage } from '@/pages/teachers/TeacherCreatePage'
import { TeacherDetailPage } from '@/pages/teachers/TeacherDetailPage'
import { CoursesListPage } from '@/pages/courses/CoursesListPage'
import { CourseCreatePage } from '@/pages/courses/CourseCreatePage'
import { CourseDetailPage } from '@/pages/courses/CourseDetailPage'
import { EnrollmentDetailPage } from '@/pages/enrollments/EnrollmentDetailPage'
import { EnrollmentsPage } from '@/pages/enrollments/EnrollmentsPage'
import { CalendarPage } from '@/pages/calendar/CalendarPage'
import { StudyGroupsListPage } from '@/pages/studyGroups/StudyGroupsListPage'
import { StudyGroupCreatePage } from '@/pages/studyGroups/StudyGroupCreatePage'
import { StudyGroupDetailPage } from '@/pages/studyGroups/StudyGroupDetailPage'
import { InvoicesListPage } from '@/pages/billing/InvoicesListPage'
import { MyInvoicesPage } from '@/pages/billing/MyInvoicesPage'
import { PayrollsListPage } from '@/pages/billing/PayrollsListPage'
import { MyPayrollsPage } from '@/pages/billing/MyPayrollsPage'
import { NotificationsPage } from '@/pages/notifications/NotificationsPage'
import { AdminNotificationsPage } from '@/pages/notifications/AdminNotificationsPage'
import { MaterialsListPage } from '@/pages/materials/MaterialsListPage'
import { StaffListPage } from '@/pages/staff/StaffListPage'
import { MyStudentsPage } from '@/pages/teachers/MyStudentsPage'

const DashboardPage = lazy(() =>
  import('@/pages/reports/DashboardPage').then((m) => ({ default: m.DashboardPage })),
)

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
              <Suspense fallback={<PageSpinner />}>
                <DashboardPage />
              </Suspense>
            </RequireAccess>
          }
        />
        <Route
          path="calendar"
          element={
            <RequireAccess permission="CanViewSchedule" roles={['Teacher', 'Student']}>
              <CalendarPage />
            </RequireAccess>
          }
        />
        <Route
          path="students"
          element={
            <RequireAccess permission="CanViewStudents">
              <StudentsListPage />
            </RequireAccess>
          }
        />
        <Route
          path="students/new"
          element={
            <RequireAccess permission="CanCreateStudents">
              <StudentCreatePage />
            </RequireAccess>
          }
        />
        <Route
          path="students/:id"
          element={
            <RequireAccess permission="CanViewStudents">
              <StudentDetailPage />
            </RequireAccess>
          }
        />
        <Route
          path="teachers"
          element={
            <RequireAccess permission="CanViewTeachers">
              <TeachersListPage />
            </RequireAccess>
          }
        />
        <Route
          path="teachers/new"
          element={
            <RequireAccess permission="CanCreateTeachers">
              <TeacherCreatePage />
            </RequireAccess>
          }
        />
        <Route
          path="teachers/:id"
          element={
            <RequireAccess permission="CanViewTeachers">
              <TeacherDetailPage />
            </RequireAccess>
          }
        />
        <Route
          path="my-students"
          element={
            <RequireAccess roles={['Teacher']}>
              <MyStudentsPage />
            </RequireAccess>
          }
        />
        <Route
          path="courses"
          element={
            <RequireAccess permission="CanViewCourses">
              <CoursesListPage />
            </RequireAccess>
          }
        />
        <Route
          path="courses/new"
          element={
            <RequireAccess permission="CanManageCourses">
              <CourseCreatePage />
            </RequireAccess>
          }
        />
        <Route
          path="courses/:id"
          element={
            <RequireAccess permission="CanViewCourses">
              <CourseDetailPage />
            </RequireAccess>
          }
        />
        <Route
          path="enrollments"
          element={
            <RequireAccess permission="CanViewEnrollments" roles={['Student']}>
              <EnrollmentsPage />
            </RequireAccess>
          }
        />
        <Route
          path="enrollments/:id"
          element={
            <RequireAccess permission="CanViewEnrollments">
              <EnrollmentDetailPage />
            </RequireAccess>
          }
        />
        <Route
          path="study-groups"
          element={
            <RequireAccess permission="CanViewSchedule">
              <StudyGroupsListPage />
            </RequireAccess>
          }
        />
        <Route
          path="study-groups/new"
          element={
            <RequireAccess permission="CanManageSchedule">
              <StudyGroupCreatePage />
            </RequireAccess>
          }
        />
        <Route
          path="study-groups/:id"
          element={
            <RequireAccess permission="CanViewSchedule">
              <StudyGroupDetailPage />
            </RequireAccess>
          }
        />
        <Route
          path="billing/invoices"
          element={
            <RequireAccess permission="CanViewPayments">
              <InvoicesListPage />
            </RequireAccess>
          }
        />
        <Route
          path="billing/payroll"
          element={
            <RequireAccess permission="CanViewPayments">
              <PayrollsListPage />
            </RequireAccess>
          }
        />
        <Route
          path="invoices"
          element={
            <RequireAccess roles={['Student']}>
              <MyInvoicesPage />
            </RequireAccess>
          }
        />
        <Route
          path="payroll"
          element={
            <RequireAccess roles={['Teacher']}>
              <MyPayrollsPage />
            </RequireAccess>
          }
        />
        <Route
          path="materials"
          element={
            <RequireAccess permission="CanViewMaterials" roles={['Teacher', 'Student']}>
              <MaterialsListPage />
            </RequireAccess>
          }
        />
        <Route path="notifications" element={<NotificationsPage />} />
        <Route
          path="notifications/admin"
          element={
            <RequireAccess permission="CanManageNotifications">
              <AdminNotificationsPage />
            </RequireAccess>
          }
        />
        <Route
          path="staff"
          element={
            <RequireAccess permission="CanManageAdmins">
              <StaffListPage />
            </RequireAccess>
          }
        />
        <Route path="profile" element={<ProfilePage />} />
        <Route path="403" element={<ForbiddenPage />} />
      </Route>

      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  )
}
