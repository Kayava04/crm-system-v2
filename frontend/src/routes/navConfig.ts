import type { LucideIcon } from 'lucide-react'
import {
  LayoutDashboard,
  Users,
  GraduationCap,
  BookOpen,
  ClipboardList,
  UsersRound,
  CalendarDays,
  Receipt,
  Wallet,
  Bell,
  FolderOpen,
  ShieldCheck,
} from 'lucide-react'
import type { Permission, Role } from '@/lib/permissions'

export interface NavItem {
  to: string
  labelKey: string
  icon: LucideIcon
  permission?: Permission
  roles?: Role[]
  end?: boolean
}

export interface NavSection {
  titleKey?: string
  items: NavItem[]
}

/** Single source of truth for the sidebar. Same list also drives which
 * permission/role each route requires (see App.tsx). */
export const navSections: NavSection[] = [
  {
    items: [
      {
        to: '/dashboard',
        labelKey: 'nav.dashboard',
        icon: LayoutDashboard,
        permission: 'CanViewReports',
      },
      {
        to: '/calendar',
        labelKey: 'nav.calendar',
        icon: CalendarDays,
        roles: ['Admin', 'Teacher', 'Student'],
        permission: 'CanViewSchedule',
      },
    ],
  },
  {
    titleKey: 'nav.students',
    items: [
      { to: '/students', labelKey: 'nav.students', icon: Users, permission: 'CanViewStudents' },
      {
        to: '/teachers',
        labelKey: 'nav.teachers',
        icon: GraduationCap,
        permission: 'CanViewTeachers',
      },
      { to: '/my-students', labelKey: 'nav.myStudents', icon: Users, roles: ['Teacher'] },
    ],
  },
  {
    items: [
      { to: '/courses', labelKey: 'nav.courses', icon: BookOpen, permission: 'CanViewCourses' },
      {
        to: '/enrollments',
        labelKey: 'nav.enrollments',
        icon: ClipboardList,
        permission: 'CanViewEnrollments',
        roles: ['Student'],
      },
      {
        to: '/study-groups',
        labelKey: 'nav.studyGroups',
        icon: UsersRound,
        permission: 'CanViewSchedule',
      },
    ],
  },
  {
    items: [
      {
        to: '/billing/invoices',
        labelKey: 'nav.invoices',
        icon: Receipt,
        permission: 'CanViewPayments',
      },
      {
        to: '/billing/payroll',
        labelKey: 'nav.payroll',
        icon: Wallet,
        permission: 'CanViewPayments',
      },
      { to: '/invoices', labelKey: 'nav.myInvoices', icon: Receipt, roles: ['Student'] },
      { to: '/payroll', labelKey: 'nav.myPayroll', icon: Wallet, roles: ['Teacher'] },
    ],
  },
  {
    items: [
      {
        to: '/materials',
        labelKey: 'nav.materials',
        icon: FolderOpen,
        permission: 'CanViewMaterials',
      },
      {
        to: '/notifications/admin',
        labelKey: 'nav.notifications',
        icon: Bell,
        permission: 'CanManageNotifications',
      },
    ],
  },
  {
    items: [
      { to: '/staff', labelKey: 'nav.staff', icon: ShieldCheck, permission: 'CanManageAdmins' },
    ],
  },
]

export function isNavItemVisible(item: NavItem, permissions: string[], roles: string[]): boolean {
  const hasPermission = item.permission ? permissions.includes(item.permission) : false
  const hasRole = item.roles ? item.roles.some((r) => roles.includes(r)) : false
  if (!item.permission && !item.roles) return true
  return hasPermission || hasRole
}
