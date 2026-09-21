import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'

type Theme = 'light' | 'dark'
const STORAGE_KEY = 'crm.theme'
const DEFAULT_THEME: Theme = 'light'

interface ThemeContextValue {
  theme: Theme
  setTheme: (theme: Theme) => void
  toggleTheme: () => void
}

const ThemeContext = createContext<ThemeContextValue | null>(null)

function readStoredTheme(): Theme {
  // Anything other than an explicit 'dark' (including a stale 'system' value from
  // before the three-way toggle was removed, or nothing stored yet) falls back to
  // the default: light-by-default on first visit, not the OS preference.
  return localStorage.getItem(STORAGE_KEY) === 'dark' ? 'dark' : DEFAULT_THEME
}

export function ThemeProvider({ children }: { children: ReactNode }) {
  const [theme, setThemeState] = useState<Theme>(readStoredTheme)

  // Synchronize the external system (the DOM class Tailwind's dark variant reads).
  useEffect(() => {
    document.documentElement.classList.toggle('dark', theme === 'dark')
  }, [theme])

  const value = useMemo<ThemeContextValue>(
    () => ({
      theme,
      setTheme(next) {
        setThemeState(next)
        localStorage.setItem(STORAGE_KEY, next)
      },
      toggleTheme() {
        setThemeState((prev) => {
          const next = prev === 'dark' ? 'light' : 'dark'
          localStorage.setItem(STORAGE_KEY, next)
          return next
        })
      },
    }),
    [theme],
  )

  return <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>
}

export function useTheme() {
  const ctx = useContext(ThemeContext)
  if (!ctx) throw new Error('useTheme must be used within ThemeProvider')
  return ctx
}
