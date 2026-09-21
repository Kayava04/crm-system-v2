import { Toaster } from 'sonner'
import { useTheme } from '@/theme/ThemeProvider'

export function AppToaster() {
  const { theme } = useTheme()
  return <Toaster theme={theme} richColors position="top-right" closeButton />
}
