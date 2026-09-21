import { useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { useSearchParams } from 'react-router-dom'
import { useAuth } from '@/features/auth/AuthProvider'
import { useHasRole } from '@/features/auth/useCan'
import { useAuthenticatedBlobUrl } from '@/lib/useAuthenticatedBlobUrl'
import { cn } from '@/lib/utils'
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar'
import { AccountTab } from './AccountTab'
import { ContactTab } from './ContactTab'
import { ProfileDataTab } from './ProfileDataTab'
import { PhotoTab } from './PhotoTab'
import { PasswordTab } from './PasswordTab'

interface Section {
  value: string
  labelKey: string
}

/** A settings-style profile page: an avatar/name header plus a vertical
 * section nav (horizontal pills on narrow screens) instead of the previous
 * horizontal Tabs switcher. Section choice still round-trips through the
 * `tab` search param, so old `/profile?tab=password` links keep working now
 * that the password shortcut was removed from the user menu dropdown. */
export function ProfilePage() {
  const { t } = useTranslation()
  const { user } = useAuth()
  const isAdmin = useHasRole('Admin')
  const hasProfile = !!user?.profile
  const [searchParams, setSearchParams] = useSearchParams()

  const sections = useMemo<Section[]>(() => {
    const list: Section[] = [{ value: 'account', labelKey: 'profile.tabs.account' }]
    if (isAdmin) list.push({ value: 'contact', labelKey: 'profile.tabs.contact' })
    if (hasProfile) list.push({ value: 'profile-data', labelKey: 'profile.tabs.profileData' })
    list.push({ value: 'photo', labelKey: 'profile.tabs.photo' })
    list.push({ value: 'password', labelKey: 'profile.tabs.password' })
    return list
  }, [isAdmin, hasProfile])

  const requestedTab = searchParams.get('tab') ?? 'account'
  const activeSection = sections.some((s) => s.value === requestedTab) ? requestedTab : 'account'

  function handleSectionChange(value: string) {
    if (value === 'account') {
      setSearchParams({}, { replace: true })
    } else {
      setSearchParams({ tab: value }, { replace: true })
    }
  }

  const avatarUrl = useAuthenticatedBlobUrl(user?.hasPhoto ? '/api/auth/me/photo' : null)
  const displayName = user?.profile?.fullName ?? user?.contact?.fullName ?? user?.email ?? ''
  const initials = (displayName || '?').slice(0, 2).toUpperCase()

  return (
    <div className="flex max-w-4xl flex-col gap-6">
      <div className="flex items-center gap-4">
        <Avatar className="size-14">
          {avatarUrl && <AvatarImage src={avatarUrl} alt="" />}
          <AvatarFallback className="text-base">{initials}</AvatarFallback>
        </Avatar>
        <div className="flex flex-col">
          <h1 className="text-2xl font-semibold tracking-tight">
            {displayName || t('profile.title')}
          </h1>
          {user?.email && <p className="text-sm text-muted-foreground">{user.email}</p>}
        </div>
      </div>

      <div className="flex flex-col gap-6 lg:flex-row">
        <nav className="flex shrink-0 flex-row flex-wrap gap-1 lg:w-48 lg:flex-col">
          {sections.map((section) => (
            <button
              key={section.value}
              type="button"
              onClick={() => handleSectionChange(section.value)}
              className={cn(
                'rounded-md px-3 py-2 text-left text-sm font-medium transition-colors',
                activeSection === section.value
                  ? 'bg-primary/10 text-primary'
                  : 'text-muted-foreground hover:bg-muted hover:text-foreground',
              )}
            >
              {t(section.labelKey)}
            </button>
          ))}
        </nav>

        <div className="min-w-0 flex-1">
          {activeSection === 'account' && <AccountTab />}
          {activeSection === 'contact' && isAdmin && <ContactTab />}
          {activeSection === 'profile-data' && hasProfile && <ProfileDataTab />}
          {activeSection === 'photo' && <PhotoTab />}
          {activeSection === 'password' && <PasswordTab />}
        </div>
      </div>
    </div>
  )
}
