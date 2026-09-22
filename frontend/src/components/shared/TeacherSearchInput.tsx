import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useQuery } from '@tanstack/react-query'
import { getTeachers } from '@/features/teachers/api'
import { useDebouncedValue } from '@/lib/useDebouncedValue'
import { Input } from '@/components/ui/input'

interface TeacherSearchInputProps {
  value: { id: string; fullName: string } | null
  onChange: (value: { id: string; fullName: string } | null) => void
  placeholder?: string
}

/** A small search-and-pick combobox for choosing a teacher by name/email,
 * used wherever a dialog needs a teacherId (there is no small bounded list
 * of teachers to render as a plain <Select>, unlike courses or groups). */
export function TeacherSearchInput({ value, onChange, placeholder }: TeacherSearchInputProps) {
  const { t } = useTranslation()
  const [query, setQuery] = useState('')
  const debouncedQuery = useDebouncedValue(query)

  const { data } = useQuery({
    queryKey: ['teachers', 'search-picker', debouncedQuery],
    queryFn: () => getTeachers({ search: debouncedQuery, page: 1, pageSize: 10 }),
    enabled: !value && debouncedQuery.length >= 2,
  })

  if (value) {
    return (
      <div className="flex items-center justify-between gap-2 rounded-md border border-input px-3 py-2 text-sm">
        <span className="min-w-0 flex-1 truncate">{value.fullName}</span>
        <button
          type="button"
          className="shrink-0 text-xs text-muted-foreground hover:text-foreground"
          onClick={() => onChange(null)}
        >
          {t('common.cancel')}
        </button>
      </div>
    )
  }

  return (
    <div className="flex flex-col gap-1">
      <Input value={query} onChange={(e) => setQuery(e.target.value)} placeholder={placeholder} />
      {data && data.items.length > 0 && (
        <div className="flex flex-col rounded-md border border-border">
          {data.items.map((teacher) => (
            <button
              key={teacher.id}
              type="button"
              className="px-3 py-1.5 text-left text-sm hover:bg-muted"
              onClick={() => {
                onChange({ id: teacher.id, fullName: teacher.fullName })
                setQuery('')
              }}
            >
              {teacher.fullName}{' '}
              <span className="text-xs text-muted-foreground">{teacher.email}</span>
            </button>
          ))}
        </div>
      )}
    </div>
  )
}
