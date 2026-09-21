import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { Download } from 'lucide-react'
import { Button } from '@/components/ui/button'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { downloadAuthorizedFile } from '@/lib/downloadAuthorizedFile'
import { ApiError } from '@/api/errors'

interface ExportMenuProps {
  buildUrl: (fileFormat: 'xlsx' | 'json', lang: 'uk' | 'en') => string
  filenamePrefix: string
}

export function ExportMenu({ buildUrl, filenamePrefix }: ExportMenuProps) {
  const { t, i18n } = useTranslation()
  const lang = i18n.language === 'en' ? 'en' : 'uk'

  async function handleExport(fileFormat: 'xlsx' | 'json') {
    try {
      await downloadAuthorizedFile(buildUrl(fileFormat, lang), `${filenamePrefix}.${fileFormat}`)
    } catch (err) {
      toast.error(
        err instanceof ApiError ? err.detail || t('common.unknownError') : t('common.networkError'),
      )
    }
  }

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button variant="outline">
          <Download />
          {t('common.download')}
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end">
        <DropdownMenuItem onSelect={() => handleExport('xlsx')}>Excel (.xlsx)</DropdownMenuItem>
        <DropdownMenuItem onSelect={() => handleExport('json')}>JSON</DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
