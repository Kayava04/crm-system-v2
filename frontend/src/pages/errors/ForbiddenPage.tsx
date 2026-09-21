import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { ShieldAlert } from 'lucide-react'
import { Button } from '@/components/ui/button'

export function ForbiddenPage() {
  const { t } = useTranslation()
  return (
    <div className="flex min-h-[60svh] flex-col items-center justify-center gap-3 px-4 text-center">
      <ShieldAlert className="size-10 text-destructive" />
      <h1 className="text-xl font-semibold">{t('errors.forbidden403Title')}</h1>
      <p className="max-w-sm text-sm text-muted-foreground">{t('errors.forbidden403Desc')}</p>
      <Button asChild className="mt-2">
        <Link to="/">{t('errors.goHome')}</Link>
      </Button>
    </div>
  )
}
