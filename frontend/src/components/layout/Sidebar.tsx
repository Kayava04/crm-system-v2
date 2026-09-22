import { NavLink } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { GraduationCap } from 'lucide-react'
import { cn } from '@/lib/utils'
import { useAuth } from '@/features/auth/useAuth'
import { navSections, isNavItemVisible } from '@/routes/navConfig'

interface SidebarNavProps {
  onNavigate?: () => void
  /** Desktop rail mode: labels are visually hidden (icon-only) until the
   * `group`-marked ancestor (the <aside> in Sidebar below) is hovered or
   * has focus within it, at which point they fade back in. The mobile
   * drawer (Header's <Sheet>) renders this with collapsible=false, always
   * showing full labels — there's no rail to collapse there. */
  collapsible?: boolean
}

export function SidebarNav({ onNavigate, collapsible = false }: SidebarNavProps) {
  const { t } = useTranslation()
  const { user } = useAuth()
  const permissions = user?.permissions ?? []
  const roles = user?.roles ?? []

  return (
    <nav
      className="flex flex-1 flex-col gap-4 overflow-x-hidden overflow-y-auto px-3 py-4"
      aria-label={t('common.appName')}
    >
      {navSections.map((section, i) => {
        const items = section.items.filter((item) => isNavItemVisible(item, permissions, roles))
        if (items.length === 0) return null
        return (
          <div key={i} className="flex flex-col gap-1">
            {items.map((item) => (
              <NavLink
                key={item.to}
                to={item.to}
                onClick={onNavigate}
                title={collapsible ? t(item.labelKey) : undefined}
                className={({ isActive }) =>
                  cn(
                    'flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium transition-colors',
                    collapsible &&
                      'justify-center gap-0 group-hover:justify-start group-hover:gap-3 group-focus-within:justify-start group-focus-within:gap-3',
                    isActive
                      ? 'bg-sidebar-accent text-sidebar-accent-foreground'
                      : 'text-sidebar-foreground hover:bg-sidebar-accent/60 hover:text-sidebar-accent-foreground',
                  )
                }
              >
                <item.icon className="size-5 shrink-0" />
                <span
                  className={cn(
                    'truncate',
                    collapsible &&
                      'w-0 opacity-0 transition-opacity delay-75 duration-150 group-hover:w-auto group-hover:opacity-100 group-focus-within:w-auto group-focus-within:opacity-100',
                  )}
                >
                  {t(item.labelKey)}
                </span>
              </NavLink>
            ))}
          </div>
        )
      })}
    </nav>
  )
}

export function Sidebar() {
  const { t } = useTranslation()
  return (
    <>
      {/* Reserves the collapsed rail's width in normal document flow — the
          <aside> itself is fixed/overlaid so expanding it on hover doesn't
          push or resize the main content. */}
      <div aria-hidden className="hidden w-16 shrink-0 lg:block" />
      <aside
        className={cn(
          'group fixed inset-y-0 left-0 z-40 hidden w-16 flex-col overflow-hidden',
          'border-r border-sidebar-border bg-sidebar transition-[width] duration-200 ease-out',
          'hover:w-64 hover:overflow-y-auto hover:shadow-xl',
          'focus-within:w-64 focus-within:overflow-y-auto focus-within:shadow-xl',
          'lg:flex',
        )}
      >
        <div className="flex h-14 shrink-0 items-center justify-center gap-0 overflow-hidden border-b border-sidebar-border px-3 group-hover:justify-start group-hover:gap-3 group-focus-within:justify-start group-focus-within:gap-3">
          <span className="flex size-6 shrink-0 items-center justify-center">
            <GraduationCap className="size-5 text-primary" />
          </span>
          <span
            className="w-0 truncate text-sm font-semibold text-sidebar-foreground opacity-0 transition-opacity delay-100 duration-150 group-hover:w-auto group-hover:opacity-100 group-focus-within:w-auto group-focus-within:opacity-100"
            title={t('common.appName')}
          >
            {t('common.appName')}
          </span>
        </div>
        <SidebarNav collapsible />
      </aside>
    </>
  )
}
