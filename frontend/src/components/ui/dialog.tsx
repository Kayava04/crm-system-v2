import * as React from 'react'
import * as DialogPrimitive from '@radix-ui/react-dialog'
import { X } from 'lucide-react'
import { cn } from '@/lib/utils'

export const Dialog = DialogPrimitive.Root
export const DialogTrigger = DialogPrimitive.Trigger
export const DialogClose = DialogPrimitive.Close

export function DialogOverlay({
  className,
  ...props
}: React.ComponentPropsWithoutRef<typeof DialogPrimitive.Overlay>) {
  return (
    <DialogPrimitive.Overlay
      className={cn(
        'scrim fixed inset-0 z-(--z-overlay) data-[state=open]:animate-fade-in data-[state=closed]:animate-fade-out',
        className,
      )}
      {...props}
    />
  )
}

export function DialogContent({
  className,
  children,
  showClose = true,
  ...props
}: React.ComponentPropsWithoutRef<typeof DialogPrimitive.Content> & { showClose?: boolean }) {
  return (
    <DialogPrimitive.Portal>
      <DialogOverlay />
      <DialogPrimitive.Content
        className={cn(
          'glass fixed [--glass-bg:var(--glass-bg-strong)] left-1/2 top-1/2 z-(--z-overlay) grid w-[calc(100%-2rem)] max-w-lg max-h-[85dvh] -translate-x-1/2 -translate-y-1/2 gap-5 overflow-y-auto rounded-2xl p-6 outline-none',
          'data-[state=open]:animate-pop-in data-[state=closed]:animate-pop-out',
          className,
        )}
        {...props}
      >
        {children}
        {showClose && (
          <DialogPrimitive.Close className="absolute right-3 top-3 flex size-8 items-center justify-center rounded-full text-muted-foreground outline-none transition-[color,background-color] hover:bg-foreground/6 hover:text-foreground focus-visible:ring-2 focus-visible:ring-ring">
            <X className="size-4" />
            <span className="sr-only">Закрити</span>
          </DialogPrimitive.Close>
        )}
      </DialogPrimitive.Content>
    </DialogPrimitive.Portal>
  )
}

export function DialogHeader({ className, ...props }: React.HTMLAttributes<HTMLDivElement>) {
  return (
    <div className={cn('flex flex-col gap-1.5 text-center sm:text-left', className)} {...props} />
  )
}
export function DialogFooter({ className, ...props }: React.HTMLAttributes<HTMLDivElement>) {
  return (
    <div
      className={cn('flex flex-col-reverse gap-2 sm:flex-row sm:justify-end', className)}
      {...props}
    />
  )
}
export function DialogTitle({
  className,
  ...props
}: React.ComponentPropsWithoutRef<typeof DialogPrimitive.Title>) {
  return (
    <DialogPrimitive.Title className={cn('pr-8 text-lg font-semibold', className)} {...props} />
  )
}
export function DialogDescription({
  className,
  ...props
}: React.ComponentPropsWithoutRef<typeof DialogPrimitive.Description>) {
  return (
    <DialogPrimitive.Description
      className={cn('text-sm text-muted-foreground', className)}
      {...props}
    />
  )
}
