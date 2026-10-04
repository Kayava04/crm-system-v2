import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Compass } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { StatusScreen } from '@/components/shared/StatusScreen'

export function NotFoundPage() {
  const { t } = useTranslation()
  return (
    <StatusScreen
      icon={Compass}
      title={t('errors.notFound404Title')}
      description={t('errors.notFound404Desc')}
      action={
        <Button asChild>
          <Link to="/">{t('errors.goHome')}</Link>
        </Button>
      }
    />
  )
}
