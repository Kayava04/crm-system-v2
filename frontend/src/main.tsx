import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { BrowserRouter } from 'react-router-dom'
import './i18n'
import './index.css'
import { ThemeProvider } from '@/theme/ThemeProvider'
import { AuthProvider } from '@/features/auth/AuthProvider'
import { ErrorBoundary } from '@/components/layout/ErrorBoundary'
import { AppToaster } from '@/components/layout/AppToaster'
import App from './App'

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 1,
      refetchOnWindowFocus: false,
    },
  },
})

const rootElement = document.getElementById('root')
if (!rootElement) throw new Error('#root element not found')

createRoot(rootElement).render(
  <StrictMode>
    <ErrorBoundary>
      <QueryClientProvider client={queryClient}>
        <BrowserRouter>
          <ThemeProvider>
            <AuthProvider>
              <App />
              <AppToaster />
            </AuthProvider>
          </ThemeProvider>
        </BrowserRouter>
      </QueryClientProvider>
    </ErrorBoundary>
  </StrictMode>,
)
