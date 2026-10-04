import { Outlet } from 'react-router-dom'
import { Sidebar } from './Sidebar'
import { Header } from './Header'

export function AppShell() {
  return (
    <div className="flex min-h-svh bg-background">
      <Sidebar />
      <div className="flex min-w-0 flex-1 flex-col">
        <Header />
        <main className="mx-auto w-full max-w-screen-2xl flex-1 px-4 pt-6 pb-12 sm:px-6 sm:pt-8 lg:px-8">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
