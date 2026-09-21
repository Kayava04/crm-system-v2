import type { LucideIcon } from 'lucide-react'
import { Navigate, Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { CalendarDays, Users, FolderOpen, ClipboardList, Wallet, ArrowRight } from 'lucide-react'
import { useAuth } from '@/features/auth/AuthProvider'
import { useCan, useHasRole } from '@/features/auth/useCan'
import { Card, CardContent } from '@/components/ui/card'

interface QuickLink {
  to: string
  labelKey: string
  icon: LucideIcon
}

const TEACHER_LINKS: QuickLink[] = [
  { to: '/calendar', labelKey: 'nav.myCalendar', icon: CalendarDays },
  { to: '/my-students', labelKey: 'nav.myStudents', icon: Users },
  { to: '/payroll', labelKey: 'nav.myPayroll', icon: Wallet },
  { to: '/materials', labelKey: 'nav.materials', icon: FolderOpen },
]

const STUDENT_LINKS: QuickLink[] = [
  { to: '/calendar', labelKey: 'nav.myCalendar', icon: CalendarDays },
  { to: '/enrollments', labelKey: 'nav.myEnrollments', icon: ClipboardList },
  { to: '/invoices', labelKey: 'nav.myInvoices', icon: Wallet },
  { to: '/materials', labelKey: 'nav.materials', icon: FolderOpen },
]

/** Everyone lands here after login. An Admin/SuperAdmin who can see the
 * dashboard is sent straight there — this page exists for Teacher/Student,
 * who hold no permissions by design and can't reach /dashboard at all, so
 * they get a small set of role-relevant quick links instead of a blank/raw
 * landing screen. */
export function HomePage() {
  const { t } = useTranslation()
  const { user } = useAuth()
  const canViewReports = useCan('CanViewReports')
  const isTeacher = useHasRole('Teacher')
  const isStudent = useHasRole('Student')

  if (!user) return null
  if (canViewReports) return <Navigate to="/dashboard" replace />

  const displayName = user.profile?.fullName || user.contact?.fullName || user.email
  const links = isTeacher ? TEACHER_LINKS : isStudent ? STUDENT_LINKS : []

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">
          {t('home.welcome', { name: displayName })}
        </h1>
        <p className="text-muted-foreground">{t(`roles.${user.roles[0]}`)}</p>
      </div>

      {links.length > 0 && (
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-4">
          {links.map((link) => (
            <Link key={link.to} to={link.to}>
              <Card className="h-full transition-colors hover:border-primary/50 hover:bg-accent/40">
                <CardContent className="flex items-center gap-3 py-5">
                  <link.icon className="size-5 shrink-0 text-primary" />
                  <span className="flex-1 text-sm font-medium">{t(link.labelKey)}</span>
                  <ArrowRight className="size-4 shrink-0 text-muted-foreground" />
                </CardContent>
              </Card>
            </Link>
          ))}
        </div>
      )}
    </div>
  )
}
