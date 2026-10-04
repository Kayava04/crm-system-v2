import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { ShieldAlert } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { StatusScreen } from '@/components/shared/StatusScreen'

export function ForbiddenPage() {
  const { t } = useTranslation()
  return (
    <StatusScreen
      icon={ShieldAlert}
      iconClassName="bg-destructive/10 text-destructive"
      className="min-h-[60svh]"
      title={t('errors.forbidden403Title')}
      description={t('errors.forbidden403Desc')}
      action={
        <Button asChild>
          <Link to="/">{t('errors.goHome')}</Link>
        </Button>
      }
    />
  )
}
