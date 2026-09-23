import { useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { KeyRound } from 'lucide-react'
import { ChangePasswordForm } from '@/features/auth/ChangePasswordForm'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'

export function ChangePasswordPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()

  return (
    <div className="flex min-h-svh items-center justify-center bg-muted/30 px-4">
      <Card className="w-full max-w-sm">
        <CardHeader className="items-center text-center">
          <div className="mb-1 flex size-11 items-center justify-center rounded-full bg-warning/20">
            <KeyRound className="size-6 text-warning-foreground" />
          </div>
          <CardTitle>{t('auth.mustChangePasswordTitle')}</CardTitle>
          <CardDescription>{t('auth.mustChangePasswordDescription')}</CardDescription>
        </CardHeader>
        <CardContent>
          <ChangePasswordForm onSuccess={() => navigate('/', { replace: true })} />
        </CardContent>
      </Card>
    </div>
  )
}
