import { useTranslation } from 'react-i18next'
import { ChevronLeft, ChevronRight } from 'lucide-react'
import { Button } from '@/components/ui/button'

interface PaginationProps {
  page: number
  totalPages: number
  totalCount: number
  onPageChange: (page: number) => void
}

export function Pagination({ page, totalPages, totalCount, onPageChange }: PaginationProps) {
  const { t } = useTranslation()
  if (totalCount === 0) return null

  return (
    <div className="flex items-center justify-between gap-4 pt-2">
      <p className="text-sm text-muted-foreground">
        {t('table.pageOf', { page, totalPages: Math.max(totalPages, 1) })} ·{' '}
        {t('table.totalCount', { count: totalCount })}
      </p>
      <div className="flex items-center gap-1">
        <Button
          variant="outline"
          size="icon"
          disabled={page <= 1}
          onClick={() => onPageChange(page - 1)}
          aria-label={t('common.back')}
        >
          <ChevronLeft className="size-4" />
        </Button>
        <Button
          variant="outline"
          size="icon"
          disabled={page >= totalPages}
          onClick={() => onPageChange(page + 1)}
          aria-label={t('table.next')}
        >
          <ChevronRight className="size-4" />
        </Button>
      </div>
    </div>
  )
}
