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
    className={cn(fieldClasses, 'flex h-9 appearance-none py-1', className)}
    {...props}
  />
))
Input.displayName = 'Input'
