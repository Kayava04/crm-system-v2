import { useState } from 'react'
import { Menu, GraduationCap } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Sheet, SheetContent, SheetTitle } from '@/components/ui/sheet'
import { Button } from '@/components/ui/button'
import { SidebarNav } from './Sidebar'
import { Breadcrumbs } from './Breadcrumbs'
import { NotificationBell } from './NotificationBell'
import { UserMenu } from './UserMenu'

export function Header() {
  const { t } = useTranslation()
  const [mobileNavOpen, setMobileNavOpen] = useState(false)

  return (
    <header className="glass-bar sticky top-0 z-(--z-sticky) flex h-14 items-center gap-3 border-b border-foreground/6 px-4 sm:px-6 lg:px-8">
      <Sheet open={mobileNavOpen} onOpenChange={setMobileNavOpen}>
        <Button
          variant="ghost"
          size="icon"
          className="lg:hidden"
          onClick={() => setMobileNavOpen(true)}
          aria-label={t('common.openMenu')}
        >
          <Menu className="size-5" />
        </Button>
        <SheetContent side="left" className="flex flex-col bg-sidebar p-0">
          <SheetTitle className="sr-only">{t('common.appName')}</SheetTitle>
          <div className="flex h-16 items-center border-b border-sidebar-border px-3">
            <div className="flex items-center gap-3 px-3 py-2.5">
              <span className="flex size-8 items-center justify-center rounded-lg bg-primary text-primary-foreground">
                <GraduationCap className="size-4.5" />
              </span>
              <span className="truncate text-sm font-semibold">{t('common.appName')}</span>
            </div>
          </div>
          <SidebarNav onNavigate={() => setMobileNavOpen(false)} />
        </SheetContent>
      </Sheet>

      <div className="min-w-0 flex-1">
        <Breadcrumbs />
      </div>

      <div className="flex items-center gap-2">
        <NotificationBell />
        <UserMenu />
      </div>
    </header>
  )
}
