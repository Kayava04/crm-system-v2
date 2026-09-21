import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { Copy } from 'lucide-react'
import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'

interface TemporaryPasswordDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  email?: string
  password: string | null
}

/** Shown once, right after a staff account is created or its password is reset —
 * the backend never returns the temporary password again after this response. */
export function TemporaryPasswordDialog({
  open,
  onOpenChange,
  email,
  password,
}: TemporaryPasswordDialogProps) {
  const { t } = useTranslation()

  async function handleCopy() {
    if (!password) return
    try {
      await navigator.clipboard.writeText(password)
      toast.success(t('staff.tempPasswordDialog.copied'))
    } catch {
      toast.error(t('common.unknownError'))
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{t('staff.tempPasswordDialog.title')}</DialogTitle>
          <DialogDescription>{t('staff.tempPasswordDialog.description')}</DialogDescription>
        </DialogHeader>
        <div className="flex flex-col gap-2">
          {email && <span className="text-sm text-muted-foreground">{email}</span>}
          <div className="flex items-center justify-between rounded-md border border-border bg-muted/40 px-3 py-2 font-mono text-sm">
            <span>{password}</span>
            <Button type="button" variant="ghost" size="sm" onClick={handleCopy}>
              <Copy className="size-4" />
              {t('staff.tempPasswordDialog.copy')}
            </Button>
          </div>
        </div>
        <DialogFooter>
          <Button onClick={() => onOpenChange(false)}>{t('staff.tempPasswordDialog.close')}</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
