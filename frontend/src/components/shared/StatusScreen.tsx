import type { ReactNode } from 'react'
import type { LucideIcon } from 'lucide-react'
import { cn } from '@/lib/utils'

interface StatusScreenProps {
  icon: LucideIcon
  /** Background + foreground classes for the icon badge. */
  iconClassName?: string
  title: ReactNode
  description: ReactNode
  action?: ReactNode
  className?: string
}

/** Full-height message for error and access states (404, 403, crash). */
export function StatusScreen({
  icon: Icon,
  iconClassName = 'bg-muted text-muted-foreground',
  title,
  description,
  action,
  className,
}: StatusScreenProps) {
  return (
    <div
      className={cn(
        'flex min-h-svh animate-rise-in flex-col items-center justify-center gap-3 px-4 text-center',
        className,
      )}
    >
      <div
        className={cn('mb-2 flex size-14 items-center justify-center rounded-2xl', iconClassName)}
      >
        <Icon className="size-7" strokeWidth={1.75} />
      </div>
      <h1 className="text-xl">{title}</h1>
      <p className="max-w-sm text-sm text-muted-foreground">{description}</p>
      {action && <div className="mt-3">{action}</div>}
    </div>
  )
}
