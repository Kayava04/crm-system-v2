export const PERMISSIONS = [
  'CanCreateStudents',
  'CanViewStudents',
  'CanManageStudents',
  'CanDeleteStudents',
  'CanCreateTeachers',
  'CanViewTeachers',
  'CanManageTeachers',
  'CanDeleteTeachers',
  'CanViewCourses',
  'CanManageCourses',
  'CanViewPayments',
  'CanManagePayments',
  'CanViewSchedule',
  'CanManageSchedule',
  'CanViewEnrollments',
  'CanManageEnrollments',
  'CanViewReports',
  'CanViewMaterials',
  'CanManageMaterials',
  'CanManageNotifications',
  'CanManageAdmins',
] as const

export type Permission = (typeof PERMISSIONS)[number]

export const ROLES = ['SuperAdmin', 'Admin', 'Teacher', 'Student'] as const
export type Role = (typeof ROLES)[number]
