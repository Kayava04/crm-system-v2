import * as React from 'react'
import { cva, type VariantProps } from 'class-variance-authority'
import { cn } from '@/lib/utils'

const alertVariants = cva('relative w-full rounded-lg border px-4 py-3 text-sm [&>svg]:size-4', {
  variants: {
    variant: {
      default: 'bg-background text-foreground border-border',
      destructive: 'border-destructive/30 bg-destructive/10 text-destructive',
      warning: 'border-warning/40 bg-warning/15 text-warning-foreground',
      success: 'border-success/30 bg-success/10 text-success',
    },
  },
  defaultVariants: { variant: 'default' },
})

export function Alert({
  className,
  variant,
  ...props
}: React.HTMLAttributes<HTMLDivElement> & VariantProps<typeof alertVariants>) {
  return <div role="alert" className={cn(alertVariants({ variant }), className)} {...props} />
}
export function AlertTitle({ className, ...props }: React.HTMLAttributes<HTMLHeadingElement>) {
  return <h5 className={cn('mb-1 font-medium leading-none', className)} {...props} />
}
export function AlertDescription({ className, ...props }: React.HTMLAttributes<HTMLDivElement>) {
  return <div className={cn('text-sm [&_p]:leading-relaxed', className)} {...props} />
}
