import { Bell } from 'lucide-react'
import { Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { getUnreadCount } from '@/features/notifications/api'
import { useTranslation } from 'react-i18next'

export function NotificationBell() {
  const { t } = useTranslation()
  const { data: count } = useQuery({
    queryKey: ['notifications', 'unread-count'],
    queryFn: getUnreadCount,
    refetchInterval: 45_000,
    staleTime: 30_000,
  })

  return (
    <Link
      to="/notifications"
      className="relative inline-flex size-9 items-center justify-center rounded-full text-muted-foreground transition-[color,background-color] hover:bg-foreground/6 hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
      aria-label={t('nav.notifications')}
    >
      <Bell className="size-5" />
      {!!count && count > 0 && (
        <span className="absolute right-0.5 top-0.5 flex h-4 min-w-4 animate-pop-in items-center justify-center rounded-full bg-destructive px-1 text-2xs font-semibold tabular-nums text-destructive-foreground ring-2 ring-background">
          {count > 99 ? '99+' : count}
        </span>
      )}
    </Link>
  )
}
