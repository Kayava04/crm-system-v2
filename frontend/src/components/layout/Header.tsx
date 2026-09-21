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
    <header className="sticky top-0 z-30 flex h-14 items-center gap-3 border-b border-border bg-background/95 px-4 backdrop-blur supports-backdrop-filter:bg-background/60">
      <Sheet open={mobileNavOpen} onOpenChange={setMobileNavOpen}>
        <Button
          variant="ghost"
          size="icon"
          className="lg:hidden"
          onClick={() => setMobileNavOpen(true)}
          aria-label={t('common.appName')}
        >
          <Menu className="size-5" />
        </Button>
        <SheetContent side="left" className="flex flex-col p-0">
          <SheetTitle className="sr-only">{t('common.appName')}</SheetTitle>
          <div className="flex h-14 items-center gap-2 border-b border-sidebar-border px-4">
            <GraduationCap className="size-5 text-primary" />
            <span className="truncate text-sm font-semibold">{t('common.appName')}</span>
          </div>
          <SidebarNav onNavigate={() => setMobileNavOpen(false)} />
        </SheetContent>
      </Sheet>

      <div className="min-w-0 flex-1">
        <Breadcrumbs />
      </div>

      <div className="flex items-center gap-1">
        <NotificationBell />
        <UserMenu />
      </div>
    </header>
  )
}
