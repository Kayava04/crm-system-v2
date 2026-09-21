import { useTranslation } from 'react-i18next'
import { ChangePasswordForm } from '@/features/auth/ChangePasswordForm'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'

export function PasswordTab() {
  const { t } = useTranslation()
  return (
    <Card className="max-w-sm">
      <CardHeader>
        <CardTitle className="text-base font-medium">{t('profile.tabs.password')}</CardTitle>
      </CardHeader>
      <CardContent>
        <ChangePasswordForm />
      </CardContent>
    </Card>
  )
}
