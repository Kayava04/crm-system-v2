import type { FieldValues, Path, UseFormSetError } from 'react-hook-form'
import type { ApiError } from '@/api/errors'

/** Maps RFC 9457 validation `errors` (400 responses) onto react-hook-form fields. */
export function applyServerValidation<T extends FieldValues>(
  setError: UseFormSetError<T>,
  error: ApiError,
) {
  if (!error.errors) return
  for (const [field, messages] of Object.entries(error.errors)) {
    const key = (field.charAt(0).toLowerCase() + field.slice(1)) as Path<T>
    setError(key, { type: 'server', message: messages.join(' ') })
  }
}
