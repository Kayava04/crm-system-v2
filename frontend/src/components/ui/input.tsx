import * as React from 'react'
import { cn } from '@/lib/utils'

export const Input = React.forwardRef<
  HTMLInputElement,
  React.InputHTMLAttributes<HTMLInputElement>
>(({ className, type, ...props }, ref) => (
  <input
    ref={ref}
    type={type}
    className={cn(
      'flex h-9 w-full appearance-none rounded-md border border-input bg-background px-3 py-1 text-sm shadow-xs transition-colors',
      // Without this, mobile Safari keeps its own native chrome inside a
      // type="date" input on top of our border/padding — the box ends up a
      // different size than a sibling text input and can overflow its
      // container instead of matching it.
      'placeholder:text-muted-foreground',
      'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:ring-offset-background',
      'disabled:cursor-not-allowed disabled:opacity-50',
      'aria-invalid:border-destructive aria-invalid:ring-destructive/30',
      className,
    )}
    {...props}
  />
))
Input.displayName = 'Input'
