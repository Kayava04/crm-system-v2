import type { ReactNode } from 'react'
import { Label } from '@/components/ui/label'

interface FieldProps {
  label: string
  hint?: string
  error?: string
  children: ReactNode
}

/** A label + control + error-message wrapper for a single form field, used
 * to keep create/edit forms consistent without pulling in a form-layout library. */
export function Field({ label, hint, error, children }: FieldProps) {
  return (
    <div className="flex flex-col gap-1.5">
      <Label>{label}</Label>
      {hint && <p className="text-xs text-muted-foreground">{hint}</p>}
      {children}
      {error && <p className="text-xs text-destructive">{error}</p>}
    </div>
  )
}
