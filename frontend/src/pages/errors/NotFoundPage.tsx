import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Compass } from 'lucide-react'
import { Button } from '@/components/ui/button'

export function NotFoundPage() {
  const { t } = useTranslation()
  return (
    <div className="flex min-h-svh flex-col items-center justify-center gap-3 px-4 text-center">
      <Compass className="size-10 text-muted-foreground" />
      <h1 className="text-xl font-semibold">{t('errors.notFound404Title')}</h1>
      <p className="max-w-sm text-sm text-muted-foreground">{t('errors.notFound404Desc')}</p>
      <Button asChild className="mt-2">
        <Link to="/">{t('errors.goHome')}</Link>
      </Button>
    </div>
  )
}
