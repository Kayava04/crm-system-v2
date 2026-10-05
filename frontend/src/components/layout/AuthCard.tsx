import type { ReactNode } from 'react'
import type { LucideIcon } from 'lucide-react'
import { cn } from '@/lib/utils'

interface AuthCardProps {
  icon: LucideIcon
  iconClassName: string
  title: ReactNode
  description: ReactNode
  children: ReactNode
}

export function AuthCard({
  icon: Icon,
  iconClassName,
  title,
  description,
  children,
}: AuthCardProps) {
  return (
    <div className="backdrop-wash flex min-h-svh items-center justify-center px-4 py-10">
      <div className="glass w-full max-w-sm animate-pop-in rounded-2xl p-8">
        <div className="mb-6 flex flex-col items-center gap-2 text-center">
          <div
            className={cn(
              'mb-2 flex size-12 items-center justify-center rounded-xl',
              iconClassName,
            )}
          >
            <Icon className="size-6" />
          </div>
          <h1 className="text-xl">{title}</h1>
          <p className="text-sm text-muted-foreground">{description}</p>
        </div>
        {children}
      </div>
    </div>
  )
}
