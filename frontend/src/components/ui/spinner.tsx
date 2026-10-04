import { useTranslation } from 'react-i18next'
import { cn } from '@/lib/utils'

export function Spinner({ className }: { className?: string }) {
  const { t } = useTranslation()
  return (
    <div
      className={cn(
        'size-6 animate-spin rounded-full border-2 border-foreground/10 border-t-primary',
        className,
      )}
      role="status"
      aria-label={t('common.loading')}
    />
  )
}
