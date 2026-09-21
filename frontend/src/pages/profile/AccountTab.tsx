import { useTranslation } from 'react-i18next'
import { useAuth } from '@/features/auth/AuthProvider'
import { useHasRole } from '@/features/auth/useCan'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'

/** Teacher and Student accounts hold zero permissions by design (their own-data
 * screens are gated by role, not by permission) - showing an always-empty
 * "Permissions" card on their profile is just noise, so it's hidden for them. */
export function AccountTab() {
  const { t } = useTranslation()
  const { user } = useAuth()
  const isTeacher = useHasRole('Teacher')
  const isStudent = useHasRole('Student')
  const showPermissions = !isTeacher && !isStudent
  if (!user) return null

  return (
    <div className="flex flex-col gap-4">
      <Card>
        <CardHeader>
          <CardTitle className="text-base font-medium">{t('profile.account.emailLabel')}</CardTitle>
        </CardHeader>
        <CardContent className="text-sm">{user.email}</CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-base font-medium">{t('profile.account.rolesLabel')}</CardTitle>
        </CardHeader>
        <CardContent className="flex flex-wrap gap-1.5">
          {user.roles.map((role) => (
            <Badge key={role} variant="secondary">
              {t(`roles.${role}`, { defaultValue: role })}
            </Badge>
          ))}
        </CardContent>
      </Card>

      {showPermissions && (
        <Card>
          <CardHeader>
            <CardTitle className="text-base font-medium">
              {t('profile.account.permissionsLabel')}
            </CardTitle>
          </CardHeader>
          <CardContent className="flex flex-wrap gap-1.5">
            {user.permissions.length === 0 && (
              <p className="text-sm text-muted-foreground">{t('profile.account.noPermissions')}</p>
            )}
            {user.permissions.map((permission) => (
              <Badge key={permission} variant="outline">
                {permission}
              </Badge>
            ))}
          </CardContent>
        </Card>
      )}
    </div>
  )
}
