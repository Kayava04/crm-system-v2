import { Link, useLocation } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { ChevronRight, Home } from 'lucide-react'
import { navSections } from '@/routes/navConfig'

const allItems = navSections.flatMap((s) => s.items)

// Routes reachable outside the sidebar (profile menu, notification bell) have
// no navConfig entry, so they'd otherwise fall back to the raw, untranslated
// path segment (e.g. "profile") instead of a real label.
const EXTRA_LABELS: Record<string, string> = {
  '/profile': 'nav.profile',
  '/notifications': 'nav.notifications',
}

/** Route-based breadcrumb (top-level segment only, from navConfig). We use a
 * plain <Routes> tree (not a data router / RouterProvider), so this can't use
 * react-router's useMatches — it's data-router only. */
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
      aria-label="Breadcrumb"
    >
      <Link to="/" className="flex items-center gap-1 hover:text-foreground">
        <Home className="size-3.5" />
      </Link>
      <ChevronRight className="size-3.5" />
      <span className="truncate font-medium text-foreground">{label}</span>
    </nav>
  )
}
