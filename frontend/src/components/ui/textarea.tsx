import * as React from 'react'
import { cn } from '@/lib/utils'
import { fieldClasses } from './field-styles'

export const Textarea = React.forwardRef<
  HTMLTextAreaElement,
  React.TextareaHTMLAttributes<HTMLTextAreaElement>
>(({ className, ...props }, ref) => (
  <textarea ref={ref} className={cn(fieldClasses, 'flex min-h-20 py-2', className)} {...props} />
))
Textarea.displayName = 'Textarea'
