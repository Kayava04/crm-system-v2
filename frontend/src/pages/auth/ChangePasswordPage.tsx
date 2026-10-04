import { useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { KeyRound } from 'lucide-react'
import { ChangePasswordForm } from '@/features/auth/ChangePasswordForm'
import { AuthCard } from '@/components/layout/AuthCard'

export function ChangePasswordPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()

  return (
    <AuthCard
      icon={KeyRound}
      iconClassName="bg-warning/20 text-warning-foreground"
      title={t('auth.mustChangePasswordTitle')}
      description={t('auth.mustChangePasswordDescription')}
    >
      <ChangePasswordForm onSuccess={() => navigate('/', { replace: true })} />
    </AuthCard>
  )
}
