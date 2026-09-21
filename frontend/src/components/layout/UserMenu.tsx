import { useTranslation } from 'react-i18next'
import { Link, useNavigate } from 'react-router-dom'
import { LogOut, User as UserIcon, Sun, Moon, Languages } from 'lucide-react'
import { useAuth } from '@/features/auth/AuthProvider'
import { useAuthenticatedBlobUrl } from '@/lib/useAuthenticatedBlobUrl'
import { useTheme } from '@/theme/ThemeProvider'
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'

function initials(name: string | null | undefined, email: string) {
  const source = name?.trim() || email
  const parts = source.split(/\s+/).filter(Boolean)
  if (parts.length >= 2) return (parts[0][0] + parts[1][0]).toUpperCase()
  return source.slice(0, 2).toUpperCase()
}

export function UserMenu() {
  const { t, i18n } = useTranslation()
  const { user, logout } = useAuth()
  const { theme, toggleTheme } = useTheme()
  const navigate = useNavigate()
  const photoUrl = useAuthenticatedBlobUrl(user?.hasPhoto ? '/api/auth/me/photo' : null)

  if (!user) return null

  const displayName = user.profile?.fullName || user.contact?.fullName || user.email
  const roleKey = user.roles[0]

  function handleLogout() {
    logout()
    navigate('/login', { replace: true })
  }

  function toggleLanguage() {
    void i18n.changeLanguage(i18n.language === 'uk' ? 'en' : 'uk')
  }

  return (
    <DropdownMenu>
      <DropdownMenuTrigger className="flex items-center gap-2 rounded-full outline-none focus-visible:ring-2 focus-visible:ring-ring">
        <Avatar>
          {photoUrl && <AvatarImage src={photoUrl} alt="" />}
          <AvatarFallback>
            {initials(user.profile?.fullName ?? user.contact?.fullName, user.email)}
          </AvatarFallback>
        </Avatar>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-64">
        <DropdownMenuLabel className="flex flex-col gap-0.5 px-2 py-1.5">
          <span className="truncate text-sm font-medium text-foreground">{displayName}</span>
          <span className="truncate text-xs text-muted-foreground">{user.email}</span>
          {roleKey && (
            <span className="mt-1 text-xs text-muted-foreground">{t(`roles.${roleKey}`)}</span>
          )}
        </DropdownMenuLabel>
        <DropdownMenuSeparator />
        <DropdownMenuItem asChild>
          <Link to="/profile">
            <UserIcon /> {t('nav.profile')}
          </Link>
        </DropdownMenuItem>
        <DropdownMenuSeparator />
        <DropdownMenuItem onSelect={toggleLanguage}>
          <Languages /> {t('common.language')}: {i18n.language === 'uk' ? 'UA' : 'EN'}
        </DropdownMenuItem>
        <DropdownMenuItem onSelect={toggleTheme}>
          {theme === 'dark' ? <Moon /> : <Sun />}
          {theme === 'dark' ? t('common.darkTheme') : t('common.lightTheme')}
        </DropdownMenuItem>
        <DropdownMenuSeparator />
        <DropdownMenuItem destructive onSelect={handleLogout}>
          <LogOut /> {t('auth.logout')}
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
