import * as React from 'react'
import { cn } from '@/lib/utils'
import { fieldClasses } from './field-styles'

export const Input = React.forwardRef<
  HTMLInputElement,
  React.InputHTMLAttributes<HTMLInputElement>
>(({ className, type, ...props }, ref) => (
  <input
    ref={ref}
    type={type}
    className={cn(
      fieldClasses,
      // Without appearance-none, mobile Safari keeps its own native chrome inside a
      // type="date" input on top of our border/padding — the box ends up a
      // different size than a sibling text input and can overflow its
      // container instead of matching it.
      'flex h-9 appearance-none py-1',
      className,
    )}
    {...props}
  />
))
Input.displayName = 'Input'
