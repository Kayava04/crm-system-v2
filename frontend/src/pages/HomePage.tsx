import type { LucideIcon } from 'lucide-react'
import { Navigate, Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import {
  ArrowRight,
  BookOpen,
  CalendarDays,
  ClipboardList,
  FolderOpen,
  Users,
  Wallet,
} from 'lucide-react'
import { useAuth } from '@/features/auth/useAuth'
import { useCan, useHasRole } from '@/features/auth/useCan'
import type { Permission } from '@/lib/permissions'
import { Card, CardContent } from '@/components/ui/card'

interface QuickLink {
  to: string
  labelKey: string
  icon: LucideIcon
  permission?: Permission
}

const TEACHER_LINKS: QuickLink[] = [
  { to: '/calendar', labelKey: 'nav.myCalendar', icon: CalendarDays },
  { to: '/my-students', labelKey: 'nav.myStudents', icon: Users },
  { to: '/courses', labelKey: 'nav.courses', icon: BookOpen, permission: 'CanViewCourses' },
  { to: '/payroll', labelKey: 'nav.myPayroll', icon: Wallet },
  { to: '/materials', labelKey: 'nav.materials', icon: FolderOpen },
]

const STUDENT_LINKS: QuickLink[] = [
  { to: '/calendar', labelKey: 'nav.myCalendar', icon: CalendarDays },
  { to: '/enrollments', labelKey: 'nav.myEnrollments', icon: ClipboardList },
  { to: '/invoices', labelKey: 'nav.myInvoices', icon: Wallet },
  { to: '/materials', labelKey: 'nav.materials', icon: FolderOpen },
]

export function HomePage() {
  const { t } = useTranslation()
  const { user } = useAuth()
  const canViewReports = useCan('CanViewReports')
  const isTeacher = useHasRole('Teacher')
  const isStudent = useHasRole('Student')

  if (!user) return null
  if (canViewReports) return <Navigate to="/dashboard" replace />

  const displayName = user.profile?.fullName || user.contact?.fullName || user.email
  const links = (isTeacher ? TEACHER_LINKS : isStudent ? STUDENT_LINKS : []).filter(
    (link) => !link.permission || user.permissions?.includes(link.permission),
  )

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1>{t('home.welcome', { name: displayName })}</h1>
        <p className="text-muted-foreground">{t(`roles.${user.roles[0]}`)}</p>
      </div>

      {links.length > 0 && (
        <div
          className={`grid grid-cols-1 gap-3 sm:grid-cols-2 ${
            links.length === 5 ? 'lg:grid-cols-3 2xl:grid-cols-5' : 'lg:grid-cols-4'
          }`}
        >
          {links.map((link) => (
            <Link
              key={link.to}
              to={link.to}
              className="group rounded-2xl outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:ring-offset-background"
            >
              <Card className="h-full transition-[border-color,box-shadow,transform] duration-200 group-hover:-translate-y-0.5 group-hover:border-primary/30 group-hover:shadow-md">
                <CardContent className="flex items-center gap-3 py-5">
                  <span className="flex size-9 shrink-0 items-center justify-center rounded-lg bg-primary/10 text-primary">
                    <link.icon className="size-4.5" />
                  </span>
                  <span className="flex-1 text-sm font-medium">{t(link.labelKey)}</span>
                  <ArrowRight className="size-4 shrink-0 text-muted-foreground transition-transform group-hover:translate-x-0.5" />
                </CardContent>
              </Card>
            </Link>
          ))}
        </div>
      )}
    </div>
  )
}
