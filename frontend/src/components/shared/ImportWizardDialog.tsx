import { useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { FileUp, CheckCircle2, XCircle } from 'lucide-react'
import type { Schemas } from '@/api/types'
import { toNum } from '@/lib/utils'
import { ApiError } from '@/api/errors'
import { downloadAuthorizedFile } from '@/lib/downloadAuthorizedFile'
import { Button } from '@/components/ui/button'
import { Checkbox } from '@/components/ui/checkbox'
import { Label } from '@/components/ui/label'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Badge } from '@/components/ui/badge'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'

type ImportResult = Schemas['ImportResponse']

interface ImportWizardDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  title: string
  templateUrl: (fileFormat: 'xlsx' | 'json', lang: 'uk' | 'en') => string
  importFn: (
    file: File,
    options: { dryRun: boolean; allOrNothing: boolean },
  ) => Promise<ImportResult>
  onImported: () => void
}

export function ImportWizardDialog({
  open,
  onOpenChange,
  title,
  templateUrl,
  importFn,
  onImported,
}: ImportWizardDialogProps) {
  const { t, i18n } = useTranslation()
  const lang = i18n.language === 'en' ? 'en' : 'uk'
  const fileInputRef = useRef<HTMLInputElement>(null)
  const [file, setFile] = useState<File | null>(null)
  const [dryRun, setDryRun] = useState(true)
  const [allOrNothing, setAllOrNothing] = useState(false)
  const [result, setResult] = useState<ImportResult | null>(null)
  const [pending, setPending] = useState(false)
  const [error, setError] = useState<string | null>(null)

  function reset() {
    setFile(null)
    setResult(null)
    setError(null)
    setDryRun(true)
    setAllOrNothing(false)
    if (fileInputRef.current) fileInputRef.current.value = ''
  }

  async function handleRun() {
    if (!file) return
    setPending(true)
    setError(null)
    try {
      const res = await importFn(file, { dryRun, allOrNothing })
      setResult(res)
      if (!dryRun && res.failed === 0) {
        toast.success(t('table.bulkDeleteResult', { succeeded: res.succeeded, total: res.total }))
        onImported()
      }
    } catch (err) {
      setError(
        err instanceof ApiError ? err.detail || t('common.unknownError') : t('common.networkError'),
      )
    } finally {
      setPending(false)
    }
  }

  return (
    <Dialog
      open={open}
      onOpenChange={(next) => {
        if (!next) reset()
        onOpenChange(next)
      }}
    >
      <DialogContent className="max-w-lg">
        <DialogHeader>
          <DialogTitle>{title}</DialogTitle>
          <DialogDescription>{t('import.description')}</DialogDescription>
        </DialogHeader>

        <div className="flex flex-col gap-4">
          <div className="flex flex-wrap gap-2">
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={() => downloadAuthorizedFile(templateUrl('xlsx', lang), 'template.xlsx')}
            >
              {t('import.downloadTemplate')} (.xlsx)
            </Button>
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={() => downloadAuthorizedFile(templateUrl('json', lang), 'template.json')}
            >
              {t('import.downloadTemplate')} (.json)
            </Button>
          </div>

          {error && (
            <Alert variant="destructive">
              <AlertDescription>{error}</AlertDescription>
            </Alert>
          )}

          <input
            ref={fileInputRef}
            type="file"
            accept=".xlsx,.json"
            className="hidden"
            onChange={(e) => {
              setResult(null)
              setFile(e.target.files?.[0] ?? null)
            }}
          />
          <Button
            type="button"
            variant="outline"
            onClick={() => fileInputRef.current?.click()}
            className="self-start"
          >
            <FileUp />
            {file ? file.name : t('import.selectFile')}
          </Button>

          <div className="flex flex-col gap-2">
            <div className="flex items-center gap-2">
              <Checkbox
                id="dryRun"
                checked={dryRun}
                onCheckedChange={(v) => setDryRun(v === true)}
              />
              <Label htmlFor="dryRun" className="font-normal">
                {t('import.dryRun')}
              </Label>
            </div>
            <div className="flex items-center gap-2">
              <Checkbox
                id="allOrNothing"
                checked={allOrNothing}
                onCheckedChange={(v) => setAllOrNothing(v === true)}
              />
              <Label htmlFor="allOrNothing" className="font-normal">
                {t('import.allOrNothing')}
              </Label>
            </div>
          </div>

          {result && (
            <div className="flex flex-col gap-2 rounded-md border border-border p-3">
              <div className="flex flex-wrap items-center gap-2 text-sm">
                <Badge variant="success">
                  {t('import.rowsSucceeded', { count: toNum(result.succeeded) })}
                </Badge>
                {toNum(result.failed) > 0 && (
                  <Badge variant="destructive">
                    {t('import.rowsFailed', { count: toNum(result.failed) })}
                  </Badge>
                )}
              </div>
              {result.ignoredColumns.length > 0 && (
                <p className="text-xs text-muted-foreground">
                  {t('import.ignoredColumns', { columns: result.ignoredColumns.join(', ') })}
                </p>
              )}
              {toNum(result.failed) > 0 && (
                <ul className="flex max-h-48 flex-col gap-1 overflow-y-auto text-xs">
                  {result.rows
                    .filter((row) => !row.success)
                    .map((row) => (
                      <li key={row.row} className="flex items-start gap-1.5 text-destructive">
                        <XCircle className="mt-0.5 size-3.5 shrink-0" />
                        <span>
                          {t('import.rowNumber')} {row.row}: {row.errors.join('; ')}
                        </span>
                      </li>
                    ))}
                </ul>
              )}
              {result.failed === 0 &&
                result.rows.map((row) => (
                  <div key={row.row} className="flex items-center gap-1.5 text-xs text-success">
                    <CheckCircle2 className="size-3.5" />
                    {t('import.rowNumber')} {row.row}
                  </div>
                ))}
            </div>
          )}
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            {t('import.close')}
          </Button>
          <Button onClick={handleRun} loading={pending} disabled={!file}>
            {pending ? (dryRun ? t('import.checking') : t('import.importing')) : t('import.run')}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
