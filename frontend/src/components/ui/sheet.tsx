import * as React from 'react'
import * as DialogPrimitive from '@radix-ui/react-dialog'
import { X } from 'lucide-react'
import { cva, type VariantProps } from 'class-variance-authority'
import { cn } from '@/lib/utils'

export const Sheet = DialogPrimitive.Root
export const SheetTrigger = DialogPrimitive.Trigger
export const SheetClose = DialogPrimitive.Close

const sheetVariants = cva('fixed z-(--z-overlay) gap-4 bg-background p-0 shadow-xl outline-none', {
  variants: {
    side: {
      left: 'inset-y-0 left-0 h-full w-72 max-w-[85vw] border-r border-border data-[state=open]:animate-slide-in-left data-[state=closed]:animate-slide-out-left',
      right:
        'inset-y-0 right-0 h-full w-80 max-w-[85vw] border-l border-border data-[state=open]:animate-slide-in-right data-[state=closed]:animate-slide-out-right',
    },
  },
  defaultVariants: { side: 'left' },
})

export function SheetContent({
  side,
  className,
  children,
  ...props
}: React.ComponentPropsWithoutRef<typeof DialogPrimitive.Content> &
  VariantProps<typeof sheetVariants>) {
  return (
    <DialogPrimitive.Portal>
      <DialogPrimitive.Overlay className="scrim fixed inset-0 z-(--z-overlay) data-[state=open]:animate-fade-in data-[state=closed]:animate-fade-out" />
      <DialogPrimitive.Content className={cn(sheetVariants({ side }), className)} {...props}>
        {children}
        <DialogPrimitive.Close className="absolute right-3 top-3 flex size-8 items-center justify-center rounded-full text-muted-foreground outline-none transition-[color,background-color] hover:bg-foreground/6 hover:text-foreground focus-visible:ring-2 focus-visible:ring-ring">
          <X className="size-4" />
          <span className="sr-only">Закрити</span>
        </DialogPrimitive.Close>
      </DialogPrimitive.Content>
    </DialogPrimitive.Portal>
  )
}
export function SheetTitle(props: React.ComponentPropsWithoutRef<typeof DialogPrimitive.Title>) {
  return <DialogPrimitive.Title {...props} />
}
export function SheetDescription(
  props: React.ComponentPropsWithoutRef<typeof DialogPrimitive.Description>,
) {
  return <DialogPrimitive.Description {...props} />
}
