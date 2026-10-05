import { Link, useLocation } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { ChevronRight, Home } from 'lucide-react'
import { navSections } from '@/routes/navConfig'

const allItems = navSections.flatMap((s) => s.items)

const EXTRA_LABELS: Record<string, string> = {
  '/profile': 'nav.profile',
  '/notifications': 'nav.notifications',
}

export function Breadcrumbs() {
  const { t } = useTranslation()
  const location = useLocation()

  const segments = location.pathname.split('/').filter(Boolean)
  if (segments.length === 0) return null

  const topPath = `/${segments[0]}`
  const navItem = allItems.find((i) => i.to === topPath)
  const labelKey = navItem?.labelKey ?? EXTRA_LABELS[topPath]
  const label = labelKey ? t(labelKey) : segments[0]

  return (
    <nav
      className="flex min-w-0 items-center gap-1.5 text-sm text-muted-foreground"
      aria-label={t('common.breadcrumbs')}
    >
      <Link
        to="/"
        className="flex items-center gap-1 rounded-md transition-colors hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
        aria-label={t('common.home')}
      >
        <Home className="size-4" />
      </Link>
      <ChevronRight className="size-3.5 text-foreground/25" aria-hidden />
      <span className="truncate font-medium text-foreground" aria-current="page">
        {label}
      </span>
    </nav>
  )
}
