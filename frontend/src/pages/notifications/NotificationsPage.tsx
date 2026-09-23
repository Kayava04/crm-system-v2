import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { CheckCheck } from 'lucide-react'
import {
  getMyNotifications,
  markNotificationRead,
  markAllNotificationsRead,
} from '@/features/notifications/api'
import type { Notification } from '@/features/notifications/api'
import { enumLabel } from '@/lib/enumLabels'
import { formatDateTime } from '@/lib/utils'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import { Checkbox } from '@/components/ui/checkbox'
import { Label } from '@/components/ui/label'
import { Pagination } from '@/components/shared/Pagination'
import { toNum } from '@/lib/utils'

export function NotificationsPage() {
  const { t, i18n } = useTranslation()
  const lang = i18n.language === 'en' ? 'en' : 'uk'
  const navigate = useNavigate()
  const queryClient = useQueryClient()

  const [unreadOnly, setUnreadOnly] = useState(false)
  const [page, setPage] = useState(1)

  const { data, isLoading } = useQuery({
    queryKey: ['notifications', 'my', unreadOnly, page],
    queryFn: () => getMyNotifications({ unreadOnly, page, pageSize: 20 }),
  })

  function invalidate() {
    queryClient.invalidateQueries({ queryKey: ['notifications'] })
  }

  async function handleOpen(n: Notification) {
    if (!n.isRead) {
      await markNotificationRead(n.id)
      invalidate()
    }
    if (n.action === 'ChangePassword') {
      navigate('/change-password')
    }
  }

  async function handleMarkAllRead() {
    await markAllNotificationsRead()
    toast.success(t('notifications.markAllReadSuccess'))
    invalidate()
  }

  return (
    <div className="mx-auto flex w-full max-w-2xl flex-col gap-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-2xl font-semibold tracking-tight">{t('notifications.title')}</h1>
        <Button variant="outline" size="sm" onClick={handleMarkAllRead}>
          <CheckCheck />
          {t('notifications.markAllRead')}
        </Button>
      </div>

      <div className="flex w-fit items-center gap-2">
        <Checkbox
          id="unreadOnly"
          checked={unreadOnly}
          onCheckedChange={(checked) => {
            setUnreadOnly(checked === true)
            setPage(1)
          }}
        />
        <Label htmlFor="unreadOnly" className="font-normal">
          {t('notifications.unreadOnly')}
        </Label>
      </div>

      {isLoading && (
        <div className="flex flex-col gap-3">
          <Skeleton className="h-20 w-full" />
          <Skeleton className="h-20 w-full" />
        </div>
      )}

      {!isLoading && (data?.items.length ?? 0) === 0 && (
        <p className="text-sm text-muted-foreground">{t('notifications.empty')}</p>
      )}

      <div className="flex flex-col gap-2">
        {data?.items.map((n) => (
          <Card
            key={n.id}
            className={
              'cursor-pointer transition-colors hover:bg-muted/40' +
              (n.isRead ? '' : ' border-primary/50 bg-primary/5')
            }
            onClick={() => handleOpen(n)}
          >
            <CardContent className="flex flex-col gap-1 py-3">
              <div className="flex items-center justify-between gap-2">
                <span className="text-sm font-medium">{n.subject}</span>
                <div className="flex items-center gap-2">
                  {!n.isRead && <Badge variant="default">•</Badge>}
                  <Badge variant="outline">{enumLabel(t, 'notificationType', n.type)}</Badge>
                </div>
              </div>
              <p className="text-sm text-muted-foreground">{n.body}</p>
              <span className="text-xs text-muted-foreground">
                {formatDateTime(n.createdAt, lang)}
              </span>
              {n.action === 'ChangePassword' && (
                <span className="w-fit text-xs font-medium text-primary">
                  {t('notifications.changePasswordAction')} →
                </span>
              )}
            </CardContent>
          </Card>
        ))}
      </div>

      {data && (
        <Pagination
          page={toNum(data.page)}
          totalPages={toNum(data.totalPages)}
          totalCount={toNum(data.totalCount)}
          onPageChange={setPage}
        />
      )}
    </div>
  )
}
